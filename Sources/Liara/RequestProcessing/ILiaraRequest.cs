﻿// Author: Prasanna V. Loganathar
// Project: Liara
// Copyright (c) Launchark Technologies. All rights reserved.
// See License.txt in the project root for license information.
// 
// Created: 8:31 AM 15-02-2014

namespace Liara.RequestProcessing
{
    public interface ILiaraRequest
    {
        LiaraRequestInfo Info { get; }
        LiaraRequestCookieCollection Cookies { get; }
        LiaraQueryString QueryString { get; }
        LiaraRequestHeaderCollection Headers { get; }
        LiaraRequestParameters Parameters { get; }
        LiaraRequestFormatProvider Format { get; set; }
        dynamic Content { get; set; }
    }
}