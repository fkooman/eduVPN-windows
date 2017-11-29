﻿/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using System.Collections.Generic;
using System.Threading;

namespace System.Collections.ObjectModel
{
    public static class Extensions
    {
        /// <summary>
        /// Loads class from a JSON string provided by API
        /// </summary>
        /// <param name="i">Loadable item</param>
        /// <param name="json">JSON string representing a dictionary of key/values with <paramref name="name"/> element</param>
        /// <param name="name">The name of the value holder element</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        public static void LoadJSONAPIResponse<T>(this ObservableCollection<T> i, string json, string name, CancellationToken ct = default(CancellationToken)) where T : ILoadableItem, new()
        {
            // Parse JSON string and get inner key/value dictionary.
            var obj = eduJSON.Parser.GetValue<Dictionary<string, object>>(
                (Dictionary<string, object>)eduJSON.Parser.Parse(json, ct),
                name);

            // Verify response status.
            if (eduJSON.Parser.GetValue(obj, "ok", out bool is_ok) && !is_ok)
                throw new APIErrorException();

            // Load collection.
            if (obj["data"] is List<object> obj2)
            {
                i.Clear();

                // Parse all items listed. Don't do it in parallel to preserve the sort order.
                foreach (var el in obj2)
                {
                    var item = new T();
                    item.Load(el);
                    i.Add(item);
                }
            }
            else
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(List<object>), obj.GetType());
        }
    }
}
