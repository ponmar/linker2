﻿using Avalonia.Controls;
using Avalonia.Input;
using Linker2.Model;
using Linker2.ViewModels;

namespace Linker2.Views;

public partial class MainView : UserControl
{
    private readonly ISessionUtils sessionUtils = ServiceLocator.Resolve<ISessionUtils>();

    public MainView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.Resolve<MainViewModel>();
    }

    private void UserControl_PointerMoved(object? sender, PointerEventArgs e)
    {
        sessionUtils.ResetSessionTime();
    }

    private void UserControl_KeyDown(object? sender, KeyEventArgs e)
    {
        sessionUtils.ResetSessionTime();
    }
}
