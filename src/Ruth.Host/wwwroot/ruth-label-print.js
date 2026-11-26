const USB_PRINTER_CLASS_CODE = 0x07;
const ZEBRA_VENDOR_ID = 0x0a5f;

export function isWebUsbSupported() {
  return typeof navigator !== "undefined" && Boolean(navigator.usb);
}

export async function printZplViaWebUsb(zplPayload, options = {}) {
  if (!isWebUsbSupported()) {
    return {
      ok: false,
      bytesSent: 0,
      error: "WebUSB is not available in this browser context.",
    };
  }

  let device = null;
  try {
    const filters = options.anyUsbPrinter
      ? [{ classCode: USB_PRINTER_CLASS_CODE }]
      : [{ vendorId: ZEBRA_VENDOR_ID }, { classCode: USB_PRINTER_CLASS_CODE }];

    device = await navigator.usb.requestDevice({ filters });
    await device.open();
    if (device.configuration === null) {
      await device.selectConfiguration(1);
    }

    const endpointInfo = findBulkOutEndpoint(device.configuration);
    if (!endpointInfo) {
      await device.close();
      return {
        ok: false,
        bytesSent: 0,
        error: "No USB bulk OUT printer endpoint was found on the selected device.",
      };
    }

    await device.claimInterface(endpointInfo.interfaceNumber);
    const bytes = new TextEncoder().encode(zplPayload ?? "");
    const transfer = await device.transferOut(endpointInfo.endpointNumber, bytes);
    await device.releaseInterface(endpointInfo.interfaceNumber);
    await device.close();

    return {
      ok: transfer.status === "ok",
      bytesSent: transfer.bytesWritten ?? bytes.byteLength,
      error: transfer.status === "ok" ? null : `USB transfer status: ${transfer.status}`,
    };
  } catch (err) {
    if (device?.opened) {
      try {
        await device.close();
      } catch {
        // Ignore close errors after failure.
      }
    }
    return {
      ok: false,
      bytesSent: 0,
      error: err instanceof Error ? err.message : "WebUSB ZPL print failed.",
    };
  }
}

export function printLabelHtmlFallback(labelTitle, elements) {
  if (typeof window === "undefined") {
    return { ok: false, error: "Browser window is unavailable." };
  }

  const popup = window.open("", "_blank", "width=520,height=320,noopener,noreferrer");
  if (!popup) {
    return { ok: false, error: "Popup window was blocked by the browser." };
  }

  const rowsHtml = Array.isArray(elements)
    ? elements
        .map((el) => {
          const invertedStyle = el.invertPrint
            ? "background:#000;color:#fff;padding:4px 8px;font-weight:700;"
            : "padding:2px 0;";
          return `<div style="${invertedStyle}">${escapeHtml(el.resolvedText ?? "")}</div>`;
        })
        .join("")
    : "";

  popup.document.write(
    `<!DOCTYPE html><html lang="en"><head><meta charset="utf-8"><title>${escapeHtml(labelTitle ?? "Badge")}</title>` +
      `<style>@page{size:4in 2in;margin:0.12in;}body{font-family:sans-serif;margin:0;padding:8px;}</style>` +
      `</head><body>${rowsHtml}</body></html>`,
  );
  popup.document.close();
  popup.focus();
  popup.print();
  return { ok: true, error: null };
}

function findBulkOutEndpoint(configuration) {
  if (!configuration?.interfaces) {
    return null;
  }

  for (const iface of configuration.interfaces) {
    for (const alt of iface.alternates ?? []) {
      for (const ep of alt.endpoints ?? []) {
        if (ep.direction === "out" && ep.type === "bulk") {
          return {
            interfaceNumber: iface.interfaceNumber,
            endpointNumber: ep.endpointNumber,
          };
        }
      }
    }
  }

  return null;
}

function escapeHtml(text) {
  return String(text)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}
