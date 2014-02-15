﻿// Author: Prasanna V. Loganathar
// Project: Liara
// Copyright (c) Launchark Technologies. All rights reserved.
// See License.txt in the project root for license information.
// 
// Created: 8:31 AM 15-02-2014

namespace Liara.ResponseProcessing
{
    public interface ILiaraResponseSynchronizer
    {
        int Priority { get; set; }
        void Synchronize(ILiaraContext context);
    }
}