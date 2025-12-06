using System.Diagnostics;
using PuppeteerSharp;

namespace ParsingSDK.Parsing;

public static class BrowserActions
{
    extension(IBrowser browser)
    {
        public async Task<IPage> GetPage()
        {
            return (await browser.PagesAsync()).First();
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