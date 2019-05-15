using System;

public class Logger{
    public static void Info(string message){
        Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]{message}");
    }
    public static void Error(string message){
        Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]{message}");
    }

    public static void Warn(string message){
        Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]{message}");
    }
}