const activeStages = new Map();

class RuthLiveKitStageElement extends HTMLElement {
  connectedCallback() {
    if (this.dataset.initialized === "true") {
      return;
    }

    this.dataset.initialized = "true";
    this.renderPlaceholder("Ready to connect to virtual room stage.");
  }

  disconnectedCallback() {
    const stageId = this.getAttribute("stage-id");
    if (stageId && activeStages.has(stageId)) {
      const state = activeStages.get(stageId);
      if (state?.room && typeof state.room.disconnect === "function") {
        state.room.disconnect();
      }
      activeStages.delete(stageId);
    }
  }

  renderPlaceholder(statusText, participantLabel = "") {
    const safeStatus = escapeHtml(statusText);
    const safeParticipant = participantLabel
      ? `<span class="ruth-room-status-pill">${escapeHtml(participantLabel)}</span>`
      : "";
    this.innerHTML =
      `<div class="ruth-livekit-stage-banner">` +
      `<strong>${safeStatus}</strong>${safeParticipant}` +
      `</div>` +
      `<div class="ruth-livekit-video-grid" data-role="tiles"></div>`;
  }
}

export function ensureLiveKitWebComponentRegistered() {
  if (
    typeof window !== "undefined" &&
    typeof customElements !== "undefined" &&
    !customElements.get("ruth-livekit-stage")
  ) {
    customElements.define("ruth-livekit-stage", RuthLiveKitStageElement);
  }
  return { ok: true };
}

export async function connectLiveKitStage(stageId, wsUrl, accessToken, displayName, role) {
  ensureLiveKitWebComponentRegistered();
  const hostEl = findStageElement(stageId);

  const stageState = {
    stageId,
    wsUrl,
    displayName: displayName || "Participant",
    role: role || "Participant",
    micEnabled: role !== "Observer",
    camEnabled: role !== "Observer",
    screenShareEnabled: false,
    connectedAtIso: new Date().toISOString(),
    transportMode: "simulated-island",
    room: null,
  };

  if (typeof window !== "undefined" && window.LivekitClient?.Room) {
    try {
      const room = new window.LivekitClient.Room({ adaptiveStream: true, dynacast: true });
      await room.connect(wsUrl, accessToken);
      stageState.room = room;
      stageState.transportMode = "livekit-webrtc";
    } catch {
      stageState.transportMode = "fallback-stage";
    }
  }

  activeStages.set(stageId, stageState);
  renderStageTiles(hostEl, stageState);

  return {
    ok: true,
    transportMode: stageState.transportMode,
    micEnabled: stageState.micEnabled,
    camEnabled: stageState.camEnabled,
    screenShareEnabled: stageState.screenShareEnabled,
  };
}

export async function toggleStageTrack(stageId, trackKind) {
  const state = activeStages.get(stageId);
  if (!state) {
    return { ok: false, enabled: false };
  }

  let nextValue = false;
  if (trackKind === "microphone") {
    state.micEnabled = !state.micEnabled;
    nextValue = state.micEnabled;
    if (state.room?.localParticipant?.setMicrophoneEnabled) {
      await state.room.localParticipant.setMicrophoneEnabled(nextValue);
    }
  } else if (trackKind === "camera") {
    state.camEnabled = !state.camEnabled;
    nextValue = state.camEnabled;
    if (state.room?.localParticipant?.setCameraEnabled) {
      await state.room.localParticipant.setCameraEnabled(nextValue);
    }
  } else if (trackKind === "screenshare") {
    state.screenShareEnabled = !state.screenShareEnabled;
    nextValue = state.screenShareEnabled;
    if (state.room?.localParticipant?.setScreenShareEnabled) {
      await state.room.localParticipant.setScreenShareEnabled(nextValue);
    }
  }

  renderStageTiles(findStageElement(stageId), state);
  return { ok: true, enabled: nextValue };
}

export async function disconnectLiveKitStage(stageId) {
  const state = activeStages.get(stageId);
  if (state?.room && typeof state.room.disconnect === "function") {
    await state.room.disconnect();
  }
  activeStages.delete(stageId);

  const hostEl = findStageElement(stageId);
  if (hostEl && typeof hostEl.renderPlaceholder === "function") {
    hostEl.renderPlaceholder("Disconnected from virtual room stage.");
  }
  return { ok: true };
}

function findStageElement(stageId) {
  if (typeof document === "undefined") {
    return null;
  }
  return document.querySelector(`ruth-livekit-stage[stage-id="${CSS.escape(String(stageId))}"]`);
}

function renderStageTiles(hostEl, state) {
  if (!hostEl) {
    return;
  }

  if (typeof hostEl.renderPlaceholder === "function") {
    hostEl.renderPlaceholder(
      `Connected (${state.transportMode})`,
      `${state.displayName} · ${state.role}`,
    );
  }

  const grid = hostEl.querySelector('[data-role="tiles"]');
  if (!grid) {
    return;
  }

  const micLabel = state.micEnabled ? "Mic On" : "Muted";
  const camLabel = state.camEnabled ? "Cam On" : "Cam Off";
  const screenLabel = state.screenShareEnabled ? "Sharing Screen" : "";
  grid.innerHTML =
    `<div class="ruth-livekit-tile">` +
    `<strong>${escapeHtml(state.displayName)}</strong>` +
    `<small>${escapeHtml(state.role)} · ${micLabel} · ${camLabel} ${screenLabel}</small>` +
    `</div>`;
}

function escapeHtml(text) {
  return String(text ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}
