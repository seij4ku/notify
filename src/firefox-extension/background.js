browser.browserAction.onClicked.addListener(async tab => {
  try {
    const [{ result }] = await browser.tabs.executeScript(tab.id, { code: `(() => { const root = document.querySelector('article, main') || document.body; return { title: document.title, url: location.href, html: root.innerHTML, text: root.innerText, capturedAt: new Date().toISOString() }; })()` });
    const response = await browser.runtime.sendNativeMessage('com.notify.desktop', { version: 1, ...result });
    if (!response.ok) throw new Error(response.error);
    console.info('Notify saved', response.path);
  } catch (error) { console.error('Notify capture failed:', error); }
});
