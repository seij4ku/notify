const captureButton = document.getElementById("capture");
const againButton = document.getElementById("again");
const status = document.getElementById("status");
const buttonText = document.getElementById("buttonText");
const scopeSelect = document.getElementById("scope");
const hint = document.getElementById("hint");
const descriptions = {
  main: "Capture the article or main content area.",
  selection: "Select text on the page before opening this menu.",
  page: "Capture all visible page content."
};

scopeSelect.addEventListener("change", () => { hint.textContent = descriptions[scopeSelect.value]; });

captureButton.addEventListener("click", async () => {
  captureButton.disabled = true;
  buttonText.textContent = "Working…";
  status.dataset.state = "working";
  status.textContent = "Reading the page…";
  let progressTimer;
  try {
    const [tab] = await browser.tabs.query({ active: true, currentWindow: true });
    if (!tab) throw new Error("No active tab was found.");
    const scope = scopeSelect.value;
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
    const [page] = await browser.tabs.executeScript(tab.id, { code });
    if (!page || (!page.text?.trim() && !page.html?.trim())) throw new Error("No page content was found. Select text or choose another range.");
    const started = Date.now();
    status.textContent = "Sending the page to Notify…";
    progressTimer = setInterval(() => {
      const seconds = Math.floor((Date.now() - started) / 1000);
      if (seconds >= 15) status.textContent = `Waiting for Notify to accept the page (${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, "0")}).`;
    }, 5000);
    const request = { version: 1, action: "process", ...page };
    if (new TextEncoder().encode(JSON.stringify(request)).length > 7_500_000) throw new Error("This page is too large. Choose Main content or Selected text.");
    const response = await browser.runtime.sendNativeMessage("com.notify.desktop", request);
    if (!response?.ok) throw new Error(response?.error || "Notify did not accept the page.");
    status.dataset.state = "success";
    status.textContent = "Page sent. Processing and review are open in Notify.";
    captureButton.hidden = true;
    againButton.hidden = false;
  } catch (error) {
    status.dataset.state = "error";
    status.textContent = `Capture failed: ${error.message || error}`;
  } finally {
    clearInterval(progressTimer);
    captureButton.disabled = false;
    buttonText.textContent = "Process with Notify";
  }
});

againButton.addEventListener("click", () => window.location.reload());
