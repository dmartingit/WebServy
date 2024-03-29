﻿using System.IO;
using WebServy.Services;

namespace WebServy.Data;

public class AppState
{
    private static string ConfigFilepath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".wsconfig");

    public Config Config { get; init; } = new(ConfigFilepath);

    public List<IService> Services { get; init; } = new() { new Element(), new WhatsApp() };
}
