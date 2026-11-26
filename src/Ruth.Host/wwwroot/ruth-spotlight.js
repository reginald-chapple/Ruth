let keydownHandler = null;

export function registerSpotlightShortcut(dotNetRef) {
  unregisterSpotlightShortcut();
  keydownHandler = (event) => {
    const isMetaK = (event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k";
    if (isMetaK) {
      event.preventDefault();
      dotNetRef.invokeMethodAsync("ToggleSpotlightFromShortcut");
      return;
    }

    if (event.key === "Escape") {
      dotNetRef.invokeMethodAsync("CloseSpotlightFromShortcut");
    }
  };
  window.addEventListener("keydown", keydownHandler);
}

export function unregisterSpotlightShortcut() {
  if (keydownHandler !== null) {
    window.removeEventListener("keydown", keydownHandler);
    keydownHandler = null;
  }
}
