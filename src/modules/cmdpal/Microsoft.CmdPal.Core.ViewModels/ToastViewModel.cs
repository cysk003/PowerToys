// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.CmdPal.Core.ViewModels.Messages;

namespace Microsoft.CmdPal.Core.ViewModels;

public partial class ToastViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string ToastMessage { get; set; } = string.Empty;
}
