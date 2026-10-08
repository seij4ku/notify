const captureButton = document.getElementById("capture");
const status = document.getElementById("status");

captureButton.addEventListener("click", async () => {
  captureButton.disabled = true;
  status.textContent = "Reading the page…";
  try {
    const [tab] = await browser.tabs.query({ active: true, currentWindow: true });
    const scope = document.getElementById("scope").value;
    const code = `(() => {
      const scope = ${JSON.stringify(scope)};
      let root, html = "", text = "";
      if (scope === "selection") {
        const selection = window.getSelection();
        text = selection.toString().trim();
        const container = document.createElement("div");
        for (let i = 0; i < selection.rangeCount; i++) container.appendChild(selection.getRangeAt(i).cloneContents());
        html = container.innerHTML;
      } else {
        root = scope === "page" ? document.body : document.querySelector("article, main") || document.body;
        html = root.innerHTML;
        text = root.innerText || root.textContent || "";
      }
      return { title: document.title, url: location.href, html, text };
    })()`;
    status.textContent = "Sending page to Claude Code…";
    const [{ result }] = await browser.tabs.executeScript(tab.id, { code });
    if (!result || (!result.text && !result.html)) throw new Error("No page content was found. Select text or choose another range.");
    const response = await browser.runtime.sendNativeMessage("com.notify.desktop", { version: 1, ...result });
    if (!response || !response.ok) throw new Error(response?.error || "Notify did not confirm the capture.");
    status.textContent = `Processed and saved: ${response.path}`;
  } catch (error) {
    status.textContent = `Capture failed: ${error.message || error}`;
  } finally {
    captureButton.disabled = false;
  }
});
