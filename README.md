# Te Reo .NET

<img align="left" width="128" height="128" alt="Te Reo Icon" src="https://github.com/user-attachments/assets/be60a961-df57-4900-b1f7-9d799da4a0dd" />
An opinionated localization system replacing <code>resx</code> files. Reo is an application that facilitates the translation process and a code generation system targeting both C# and TypeScript. Reo works best on .NET 8+ projects, taking advantage of modern collections when available, but is usable for any project targeting <code>netstandard2.0</code> and newer.

<br/><br/>

## Features
- Fast & smart search in keys and values (translations) thanks to [FastDex](https://github.com/lofcz/fastdex).
- Tracks dates the keys were added and touched for easy review of the translations changed in a given period.
- Automatically infers keys based on the primary value, after memorizing the conventions used, navigating the source code is much easier than with conventional localization systems.
- Atomic depth of translations - one page can use multiple languages simultaneously for different frames if needed.
- Familiar interface, swap `Resource.Key` for `Reo.Key` and you are good to go.
- Generate translations with one click (DeepL).
- Performs `~3x` better than `resx` on .NET 8+ at runtime (measured on a project with several thousand keys).
- Group keys into logical groups and set codegen targets on each group separately.
- Strongly typed TS codegen with full intellisense that works for frontend just as amazingly as `Reo.KeyName` for backend, while loading keys lazily at runtime (`reo.require("groupName")`, `reo.KeyName`).
- [WYSIWYG editor](https://github.com/lofcz/tiptap-blazor) for keys with markup.
- Smart codegen supporting `MarkupString` for projects using Blazor.
- Standalone application for managing translations, usable as a web app or a native MAUI executable.
- Infinite undo/redo with full history jumping.
- First-class support for keys that should be excluded from the auto-translation process, such as image paths.
- Key sharing support between multiple frontend groups (and up to one backend group) to avoid collisions.
