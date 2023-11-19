using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace TamuBusFeed.Models;

public partial class Stop : ObservableObject
{
    [ObservableProperty]
    private string _key;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _stopCode;

    [ObservableProperty]
    private bool _isTemporary;

    [ObservableProperty]
    private List<object> _attributes;
}
