# MimeHtml2Html
Converts mhtml files to simple HTML

## Synopsys

Now mht is officially dead and cannot be opened in any modern browser.
On the other hand, there are a lot of legacy mht documents, and, what 
is even more relevant, puppeteer can save pages in this format only.

But why is this all for if we have an good ol' html with data URIs ?

So here is a converter that takes mht bundle and generates a single
html file that can be opened in any browser.



## Usage

```c#
using Itage.MimeHtml2Html;

// ... 


var converter = new MimeConverter(loggerFactory.CreateLogger<MimeConverter>());

using FileStream sourceStream = File.OpenRead("page.mht");
using FileStream destinationStream = File.Open("page.html", FileMode.Create);

bool result = await converter.Convert(sourceStream, destinationStream);
if (!result)
{
    Environment.Exit(-1);
}


```