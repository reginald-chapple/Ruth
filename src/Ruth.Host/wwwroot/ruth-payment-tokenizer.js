export function isHostedTokenizerReady() {
  return typeof window !== "undefined";
}

export function tokenizeHostedPayment(options = {}) {
  const methodKind = options.methodKind === "AchBankAccount" ? "AchBankAccount" : "CreditCard";
  const last4Digits = sanitizeLast4(
    options.last4Hint,
    methodKind === "AchBankAccount" ? "6789" : "4242",
  );
  const simulateDecline = Boolean(options.simulateDecline);
  const randomNonce = generateBrowserNonce();
  const prefix = simulateDecline ? "tok_decline" : "tok_hosted";

  if (methodKind === "AchBankAccount") {
    return {
      ok: true,
      hostedPaymentToken: `${prefix}_ach_${randomNonce}`,
      methodKind,
      maskedLast4: last4Digits,
      cardBrand: options.bankName || "ACH Checking",
      expiryMonth: null,
      expiryYear: null,
      error: null,
    };
  }

  const nowYear = new Date().getUTCFullYear();
  const expiryMonth = clampInteger(options.expiryMonth, 1, 12, 12);
  const expiryYear = clampInteger(options.expiryYear, nowYear, 2099, nowYear + 3);

  return {
    ok: true,
    hostedPaymentToken: `${prefix}_card_${randomNonce}`,
    methodKind,
    maskedLast4: last4Digits,
    cardBrand: options.cardBrand || "Visa",
    expiryMonth,
    expiryYear,
    error: null,
  };
}

function sanitizeLast4(raw, fallback) {
  if (typeof raw !== "string") {
    return fallback;
  }

  const digits = raw.replaceAll(/\D/g, "");
  return digits.length >= 4 ? digits.slice(-4) : fallback;
}

function clampInteger(value, min, max, fallback) {
  const parsed = Number.parseInt(String(value ?? ""), 10);
  if (Number.isNaN(parsed) || parsed < min || parsed > max) {
    return fallback;
  }

  return parsed;
}

function generateBrowserNonce() {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID().replaceAll("-", "").slice(0, 16);
  }

  return Math.random().toString(36).slice(2, 14);
}
