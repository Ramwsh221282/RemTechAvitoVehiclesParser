using System.Diagnostics;
using PuppeteerSharp;

namespace RemTechAvitoVehiclesParser.Parsing;

public static class BrowserActions
{
    extension(IBrowser browser)
    {
        public async Task Invoke(Func<IBrowser, Task> invoke)
        {
            await invoke(browser);
        }
        
        public async Task<U> Invoke<U>(Func<IBrowser, Task<U>> invoke)
        {
            return await invoke(browser);
        }

        public async Task<IPage> GetPage()
        {
            IPage[] pages = await browser.PagesAsync();
            return pages.First();
        }

        public void Destroy()
        {
            int browserProcessId = browser.Process.Id;
            Process process = Process.GetProcessById(browserProcessId);
            process.Kill();
            browser.Dispose();
        }

        public async Task DestroyAsync()
        {
            int browserProcessId = browser.Process.Id;
            Process process = Process.GetProcessById(browserProcessId);
            process.Kill();
            await process.WaitForExitAsync();
            await browser.DisposeAsync();
        }
    }
}