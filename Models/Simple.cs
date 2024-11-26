using System;

namespace Alga.xaml.Models;

public class Simple
{
    public string name { get; set; } = string.Empty;
    /// <summary>
    /// start index in doc file
    /// </summary>
    public int start { get; set; }
    /// <summary>
    /// finish index in doc file
    /// </summary>
    public int finish { get; set; }
    /// <summary>
    /// row index in list. of the opening node
    /// </summary>
    public int open { get; set; }  = -1; // Инициализация значений по умолчанию
    /// <summary>
    /// row index in list. of the close node
    /// </summary>
    public int close { get; set; } = -1; // Инициализация значений по умолчанию
    public int depth { get; set; }  // Инициализация значений по умолчанию
}