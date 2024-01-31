﻿using Castle.MicroKernel;

namespace Linker2;

public class ServiceLocator
{
    public static T Resolve<T>()
    {
        return Bootstrapper.Container.Resolve<T>();
    }

    public static T Resolve<T>(string key, object? value)
    {
        if (value is null)
        {
            return Resolve<T>();
        }

        var a = new Arguments
        {
            { key, value }
        };
        return Bootstrapper.Container.Resolve<T>(a);
    }
}
