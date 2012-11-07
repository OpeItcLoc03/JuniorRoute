---
layout: documentation_1_0
title: Introduction - JuniorRoute
documentationroot: documentation/1.0/
---
What is JuniorRoute?
=
JuniorRoute is a routing framework for .NET. JuniorRoute's simple approach to routing is a better fit for the stateless HTTP protocol than controller-driven architectures like ASP.net MVC. JuniorRoute simplifies the routing of an HTTP request message to a code method while providing lots of different ways to control how that occurs. JuniorRoute's simplest configurations involve very little bootstrapping code, but it also provides configurability and extensibility just where you need it.

Why should I use it?
=
If you're interested in a routing framework that's easy to learn and easy to use then you'll love JuniorRoute.

**JuniorRoute is designed to "just work."**

There are several features a good routing framework should provide:
* Efficient routing of HTTP messages to the appropriate code method
* Easy to configure
* Easy to extend
* Doesn't force unnecessary dependencies on developers
* Provides built-in goodies ([bundling]({{ page.documentationroot }}bundles.html), [minification]({{ page.documentationroot }}asset_transformers.html), [diagnostics]({{ page.documentationroot }}diagnostics.html), etc.) for developer productivity and best-practice support

Judged by these standards, JuniorRoute is an excellent routing implementation. However, other routing frameworks fail to deliver on one of more of these points.

Is JuniorRoute a MVC implementation?
=
No. JuniorRoute is a [front controller pattern](http://en.wikipedia.org/wiki/Front_Controller_pattern) rather than a model-view-controller implementation. Acting as a front controller, JuniorRoute handles all incoming requests and routes to the appropriate code methods. JuniorRoute does not provide, nor is there any need to code, a controller in the MVC sense. With JuniorRoute, classes act as mere method containers and class constructors are only used to deliver dependencies using dependency injection. Developers are free to use whatever class hierarchies they wish, so long as the container classes can be created by either the provided [```IContainer```]({{ page.documentationroot }}containers.html) implementation or by the runtime activator.

Is JuniorRoute IoC-container-friendly?
=
Yes. Not only is it friendly, but JuniorRoute has no forced or default IoC container dependency; developers are only required to either implement the bare-bones ```IContainer``` interface or use one of JuniorRoute's default ```IContainer``` implementations.

Is JuniorRoute dependent on ASP.net MVC?
=
No. JuniorRoute is not dependent on any ASP.net MVC assemblies. However, it does integrate with the ASP.net pipeline; JuniorRoute was not created to replace the entire ASP.net stack.

What dependencies does JuniorRoute have?
=
JuniorRoute strives to reduce third-party library dependency as much as possible. To that end, you'll be pleased to read that JuniorRoute has very few dependencies.

JuniorRoute's core functionality is dependent on the following libraries, excluding standard .NET 4.0 assemblies:
* Json.NET
* JuniorCommon

JuniorRoute's diagnostics assemblies are dependent on the following libraries, also excluding standard .NET 4.0 assemblies:
* Json.NET
* JuniorCommon
* Spark

What are JuniorRoute's system requirements?
=
For production and developer use, JuniorRoute has the following system requirements:
* .NET Framework 4.0
* Any processor architecture