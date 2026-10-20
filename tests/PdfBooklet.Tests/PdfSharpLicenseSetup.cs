using System.Runtime.CompilerServices;
using PdfSharp;

namespace PdfBooklet.Tests;

internal static class PdfSharpLicenseSetup
{
    [ModuleInitializer]
    internal static void Initialize() => PdfSharpLicense.SetAgplLicense();
}
