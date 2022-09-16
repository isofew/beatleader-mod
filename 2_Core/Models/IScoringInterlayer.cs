﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatLeader.Models
{
    public interface IScoringInterlayer
    {
        T Convert<T>(ScoringData data) where T : ScoringElement;
    }
}
