﻿using Microsoft.Win32;
using System;

namespace SiMay.Core
{

    public class RegValueHelper
    {

        public static bool SetValue(string node, string value)
        {
            bool result = true;
            try
            {
                RegistryKey key = Registry.CurrentUser;
                RegistryKey software = key.CreateSubKey("SYSTEM\\SoftWare\\SiMayService");
                software.SetValue(node, value);
            }
            catch
            {
                result = false;
            }

            return result;
        }

        public static string GetValue(string node)
        {
            string value = "";

            try
            {
                RegistryKey key = Registry.CurrentUser;
                RegistryKey software = key.OpenSubKey("SYSTEM\\SoftWare\\SiMayService");
                value = software.GetValue(node).ToString();
            }
            catch(Exception e)
            {

            }

            return value;
        }
    }
}