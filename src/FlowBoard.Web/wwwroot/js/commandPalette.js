let dotNetRef;
let keydownHandler;

export function register(reference) {
  unregister();
  dotNetRef = reference;
  keydownHandler = (event) => {
    const key = event.key?.toLowerCase();
    if ((event.ctrlKey || event.metaKey) && key === "k") {
      event.preventDefault();
      dotNetRef.invokeMethodAsync("OpenFromShortcut");
    }
  };

  document.addEventListener("keydown", keydownHandler);
}

export function unregister() {
  if (keydownHandler) {
    document.removeEventListener("keydown", keydownHandler);
  }

  keydownHandler = undefined;
  dotNetRef = undefined;
}
