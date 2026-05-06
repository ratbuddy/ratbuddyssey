// AudysseyOne post-process logic
//
// The transforms in this file (force flags off, replace responseData with
// 16384-sample impulse stubs, snap crossover/rolloff/level/distance, etc.)
// are derived from the AudysseyOne web tool by ObsessiveCompulsiveAudiophile,
// which is MIT-licensed:
//   https://github.com/ObsessiveCompulsiveAudiophile/AudysseyOne
//   Copyright (c) 2024 ObsessiveCompulsiveAudiophile
// Used here under the same MIT license terms as the rest of Ratbuddyssey.

using System;
using System.Collections.Generic;
using System.Globalization;
using Audyssey.MultEQApp;

namespace Audyssey;

/// <summary>
/// Implements AudysseyOne's "quick fix" pass on a loaded calibration:
/// disables post-processing flags the receiver applies on top of MultEQ,
/// replaces the measured impulse responses with neutral stubs (so the AVR
/// re-derives a clean filter), and snaps per-channel parameters to safe values.
/// </summary>
/// <remarks>
/// <para>
/// This is a destructive transform — it overwrites measured response data and
/// resets several user-facing flags. The UI gates it behind a confirmation
/// prompt; callers in code should expect the model to be mutated in-place.
/// </para>
/// <para>
/// Behavior mirrors AudysseyOne's "ProcessQuickFix" pass: 16384-sample
/// <c>[1, 0, 0, ...]</c> impulse stubs per measurement position; subwoofers
/// get <c>frequencyRangeRolloff = 250</c> and zeroed trim/delay; satellites
/// get <c>frequencyRangeRolloff = 20000</c>, <c>midrangeCompensation = false</c>,
/// <c>customSpeakerType = "S"</c>, and <c>channelReport.enSpeakerConnect = 1</c>.
/// </para>
/// </remarks>
public static class AudysseyOnePostProcess
{
    private const int ImpulseLength = 16384;

    public static void Apply(AudysseyMultEQApp app)
    {
        if (app == null) return;

        // 1. Force the receiver-side post-process flags off so the file plays
        //    back exactly the curve we computed (no extra DynamicEQ tilt etc.).
        app.DynamicVolume = false;
        app.Lfc = false;
        app.DynamicEq = false;
        app.EnTargetCurveType = 1;

        if (app.DetectedChannels == null) return;

        string[] impulse = BuildImpulseStub();

        foreach (var ch in app.DetectedChannels)
        {
            if (ch == null) continue;
            ApplyChannel(ch, impulse);
        }
    }

    private static void ApplyChannel(DetectedChannel ch, string[] impulseStub)
    {
        // Replace each measurement position's response with a clean impulse
        // (so the AVR re-derives a fresh, EQ-flat filter on its end).
        if (ch.ResponseData != null)
        {
            var keys = new List<string>(ch.ResponseData.Keys);
            foreach (var key in keys)
            {
                ch.ResponseData[key] = (string[])impulseStub.Clone();
            }
        }

        if (AudysseyHardwareQuirks.IsSubwoofer(ch))
        {
            ch.FrequencyRangeRolloff = 250m;
            ch.TrimAdjustment = "0";
            ch.DelayAdjustment = "0";
        }
        else
        {
            ch.MidrangeCompensation = false;
            ch.FrequencyRangeRolloff = 20000m;
            ch.CustomSpeakerType = "S";
            if (ch.ChannelReport != null)
            {
                ch.ChannelReport.EnSpeakerConnect = 1;
            }
        }
    }

    private static string[] BuildImpulseStub()
    {
        var stub = new string[ImpulseLength];
        stub[0] = "1";
        string zero = "0";
        for (int i = 1; i < ImpulseLength; i++) stub[i] = zero;
        return stub;
    }
}
