// Shell-Dev Theme Engine — Firefox background script
// Connects to the native messaging host, receives theme updates,
// and applies them live via browser.theme.update().

(function () {
  const NATIVE_HOST = "theme_engine";
  const RECONNECT_DELAY_MS = 5000;
  let port = null;

  function reloadNewTabs() {
    // about:newtab/about:home URLs are hidden from extensions; query all tabs
    // and reload any whose title indicates they're a new tab page
    browser.tabs.query({}).then((tabs) => {
      for (const tab of tabs) {
        const url = tab.url || "";
        const title = (tab.title || "").toLowerCase();
        if (url.startsWith("about:") || title === "new tab" || title === "firefox" || title === "") {
          browser.tabs.reload(tab.id).catch(() => {});
        }
      }
    }).catch(() => {});
  }

  function applyTheme(theme) {
    console.log("Theme Engine: applying theme", JSON.stringify(theme).substring(0, 200));
    browser.theme.update(theme).then(() => {
      console.log("Theme Engine: theme applied successfully");
      reloadNewTabs();
    }).catch((err) => {
      console.error("Theme Engine: failed to update theme:", err);
    });
  }

  function connect() {
    try {
      port = browser.runtime.connectNative(NATIVE_HOST);
    } catch (err) {
      console.warn("Theme Engine: could not connect to native host:", err);
      scheduleReconnect();
      return;
    }

    port.onMessage.addListener((msg) => {
      if (msg && msg.colors) {
        applyTheme(msg);
      }
    });

    port.onDisconnect.addListener((p) => {
      const err = p.error || browser.runtime.lastError;
      console.warn("Theme Engine: disconnected —", err?.message || "unknown");
      port = null;
      scheduleReconnect();
    });
  }

  function scheduleReconnect() {
    setTimeout(connect, RECONNECT_DELAY_MS);
  }

  connect();
})();
