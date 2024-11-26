using System;

namespace Alga.xaml.Models;

internal class D
{
    internal string name { get; set; } = string.Empty;
    internal int startIndex { get; set; }
    internal int endIndex { get; set; }
    /// <summary>
    /// False - Is open descriptor = <...>
    /// True - Is close descriptor (</...>) or if descriptor has not closed '>' (<... />) 
    /// </summary>
    internal bool type { get; set; }
}
