# Solicen.RPAExtractorCSharp

[**Englsih**](/README.md) | [**Русский**](./docs/ru/README.ru.md)

Made with ❤️ for **all** translators and translation developers.

This a script/tool to extract files from the **RPA** archive format from the [`Ren'Py`](https://www.renpy.org) Visual Novel Engine. Unlike its predecessors as a [unRPA](https://github.com/Lattyware/unrpa), it is written in pure C# *(CSharp)* without using Python parts in any code line.

## Using:
* Download a source code. And copy/paste two `.cs` files from `/src` folder into your project.
    And using this in your project or code, or program with:
    ```csharp
    using Solicen.RenPy
    Archive.ExtractArchive("path_to_RPA");
    ```
* Or [download](https://github.com/SolicenTEAM/RPAExtractorCSharp/releases) a command tool for extraction files from the RPA archive.

## Contributions:
* You can create your own fork of this project and contribute to its development.
* You can also contribute via the [`Issues`](https://github.com/SolicenTEAM/RPAExtractorCSharp/issues) and [`Pull Request`](https://github.com/SolicenTEAM/RPAExtractorCSharp/pulls) tabs by suggesting your code changes. And further development of the project. 

## Future of Project:
The initial state of the code and the project involved adding files to `RPA` archives, as well as creating an `RPA` archive based on files to eliminate the use of `unRPA` in CSharp projects. 

However, the original developers: [Denis Solicen](https://github.com/DenisSolicen) and [SAn4Es-TV](https://github.com/SAn4Es-TV) did not find a good solution to pack the archives back, since this would have required using parts of the Python code, which completely contradicts the original idea of the project.


## All rights reserved by us for You
You can use this project/script anywhere you want with an indication of authorship ([Denis Solicen](https://github.com/DenisSolicen) & [SAn4Es-TV](https://github.com/SAn4Es-TV)) in accordance with the MIT license. You can also freely modify this project, create forks, and interact with the source code in any way to continue and improve the project after us.

---
We express our great gratitude to the author of `unRPA` for the open source code on the basis of which this project was created in the CSharp language.

---

These wonderful people have made an invaluable contribution to the project:

<a href="https://github.com/SolicenTEAM/RPAExtractorCSharp/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=SolicenTEAM/RPAExtractorCSharp" />
</a>