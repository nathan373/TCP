﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationsLibrary
{
    public interface ILog
    {
       void WriteLine(string lineToWrite);
       void Close();
    }
}
