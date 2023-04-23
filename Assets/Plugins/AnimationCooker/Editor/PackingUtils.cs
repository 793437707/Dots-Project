// this is collection of useful static math functions for packing/unpacking and scaling
//--------------------------------------------------------------------------------------------------//


using Unity.Mathematics;

public static class PackingUtils
{
    public const float INV_BYTE = 1f / 255f; // inverse byte
    static float4 bitEnc = new float4(1f, 255f, 65025f, 16581375f);
    static float4 bitMsk = new float4(1f / 255f, 1f / 255f, 1f / 255f, 0f);
    static float4 bitDec = 1.0f / bitEnc;

    // given val, min, and max, produces a packed value stuffed into a Vector4, where x is r, y is g, z is b, and w is a.
    // note that z and x will be stored with 11 bits, and y will be stored with 10 bits.
    // each of the resulting vector's components will be between 0 and 1
    // 00 01 02 03 04 05 06 07 08 09 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31
    // |--------------11 bits (x)-----| |-----------10 bits (y)-----| |----------11 bits (z) --------|
    public static float4 PackThree10BitFloatsToARGB(float3 val, float minVal, float maxVal)
    {
        uint scaledX = (uint)Scale(val.x, minVal, maxVal, 0f, 2047.999f); // x is 11 bits
        uint scaledY = (uint)Scale(val.y, minVal, maxVal, 0f, 1023.999f); // y is only 10 bits
        uint scaledZ = (uint)Scale(val.z, minVal, maxVal, 0f, 2047.999f); // z is 11 bits

        // X11Y10Z11
        uint x = scaledX << 21; // 32 - 11 --> 21, moves it to first 10 bits
        uint y = scaledY << 11; // 21 - 10 --> 11, moves it to middle 10 bits
        uint z = scaledZ; // last 11 bits
        uint packed = (x | y | z);
        return PackUintToRGBA(packed);
    }

    public static half3 PackThree5BitFloatsToRGBHalf(float3 val, float minVal, float maxVal)
    {
        ushort scaledX = (ushort)Scale(val.x, minVal, maxVal, 0f, 31.999f); // x is 5 bits
        ushort scaledY = (ushort)Scale(val.y, minVal, maxVal, 0f, 31.999f); // y is 5 bits
        ushort scaledZ = (ushort)Scale(val.z, minVal, maxVal, 0f, 31.999f); // z is 5 bits

        ushort x = (ushort)(scaledX << 5);
        ushort y = (ushort)(scaledY << 5);
        ushort z = scaledZ;
        ushort packed = (ushort)(x | y | z);
        return PackUshortToRGBAHalf(packed);
    }

    public static float4 PackSix5BitFloatsToARGB(float3 val1, float3 val2, float minVal, float maxVal)
    {
        uint scaledX1 = (uint)Scale(val1.x, minVal, maxVal, 0f, 31.999f); // x is 5 bits
        uint scaledY1 = (uint)Scale(val1.y, minVal, maxVal, 0f, 31.999f); // y is 5 bits
        uint scaledZ1 = (uint)Scale(val1.z, minVal, maxVal, 0f, 31.999f); // z is 5 bits

        uint scaledX2 = (uint)Scale(val2.x, minVal, maxVal, 0f, 31.999f); // x is 5 bits
        uint scaledY2 = (uint)Scale(val2.y, minVal, maxVal, 0f, 31.999f); // y is 5 bits
        uint scaledZ2 = (uint)Scale(val2.z, minVal, maxVal, 0f, 31.999f); // z is 5 bits

        // X5Y5Z5X5Y5Z5
        uint x1 = scaledX1 << 27; // 32 - 5 --> 27
        uint y1 = scaledY1 << 22; // 27 - 5 --> 22
        uint z1 = scaledZ1 << 17; // 22 - 5 --> 17
        uint x2 = scaledX2 << 12; // 17 - 5 --> 12
        uint y2 = scaledY2 << 7; // 12 - 5 --> 7
        uint z2 = scaledZ2;
        uint packed = (x1 | y1 | z1 | x2 | y2 | z2);
        return PackUintToRGBA(packed);
    }

    // tangent is a value where x, y, and z are part of a vector, and the sign of w indicates the direction.
    // i am pretty sure that tangent is normalized to be between -1 and 1
    // x, y, and z will have 10 bits. w will use two bits.
    public static float4 PackTangentToARGB(float4 val, float minVal, float maxVal)
    {
        sbyte sign = (sbyte)val.z;
        uint scaledX = (uint)Scale(val.x, minVal, maxVal, 0, 1023.999f);
        uint scaledY = (uint)Scale(val.y, minVal, maxVal, 0, 1023.999f);
        uint scaledZ = (uint)Scale(val.z, minVal, maxVal, 0, 1023.999f);

        // X10Y10Z10W2 (W is sign)
        uint x = scaledX << 22; // 32 - 10 --> 22, moves it to first 10 bits
        uint y = scaledY << 12; // 22 - 10 --> 12, moves it to the next 10 bits
        uint z = scaledZ << 2; // 12 - 10 --> 2, moves it to the next 10 bits

        // will be either -1 or 1.
        // to avoid a branch, we scale w from unit range (-1..1) to unit interval (0..1).
        // (-1 + 1) * 0.5 --> 0
        // (1 + 1) * 0.5 --> 1
        uint w = (uint)ScaleUnitRangeToUnitInterval(val.z); // last two bytes

        // combine the values and return them as a packed RGBA
        uint packed = (x | y | z | w);
        return PackUintToRGBA(packed);
    }

    // same as above, but with individual parameters for values
    public static float4 PackThree10BitFloatsToARGB(float val1, float val2, float val3, float minVal, float maxVal)
    {
        return PackThree10BitFloatsToARGB(new float3(val1, val2, val3), minVal, maxVal);
    }

    // pack one 32 bit float into an RGBA vector.
    // the RGBA values will be between 0 and 1.
    public static float4 PackOne32bitFloatToRGBA(float val, float min, float max)
    {
        float scaled = ScaleToUnitInterval(val, min, max);
        float4 enc = bitEnc * scaled;
        enc = math.frac(enc);
        enc -= enc.yzww * bitMsk;
        return enc;
    }

    // unpacks a single RGB value to a float, given a range
    public static float UnpackRGBAToOne32bitFloat(float4 val, float min, float max)
    {
        float unscaled = math.dot(val, bitDec);
        return ScaleFromUnitInterval(unscaled, min, max);
    }

    // pack two 16 bit floats into an RGBA vector
    // the RGBA values will be between 0 and 1
    public static float4 PackTwo16bitFloatsToRGBA(float val1, float val2)
    {
        return PackUintToRGBA((math.f32tof16(val1) << 16) | math.f32tof16(val2));
    }

    // same as the over version except that the value is a float2
    public static float4 PackTwo16bitFloatsToRGBA(float2 val)
    {
        return PackTwo16bitFloatsToRGBA(val.x, val.y);
    }

    // unpack an rgba vector into two 16 bit floats
    // the RGBA vector must have component values that are between 0 and 1
    public static float2 UnpackRGBAToTwo16bitFloats(float4 val)
    {
        uint input = UnpackRGBAToUint(val);
        return new float2(math.f16tof32(input >> 16), math.f16tof32(input & 0xFFFF));
    }

    // pack four 8 bit bytes into an RGBA vector.
    // basically, it just multiplies each value by 1/255
    public static float4 PackFourBytesToRGBA(byte val1, byte val2, byte val3, byte val4)
    {
        return new float4(val1, val2, val3, val4) * INV_BYTE;
    }

    // scales val, which is between oldMin and oldMax, to a number between newMin and newMax
    public static float Scale(float val, float oldMin, float oldMax, float newMin, float newMax)
    {
        return (((val - oldMin) / (oldMax - oldMin)) * (newMax - newMin)) + newMin;
    }

    // scales val, which is between oldMin and oldMax, to a vector between newMin and newMax
    public static float3 Scale(float3 val, float3 oldMin, float3 oldMax, float3 newMin, float3 newMax)
    {
        return new float3(Scale(val.x, oldMin.x, oldMax.x, newMin.x, newMax.x), Scale(val.y, oldMin.y, oldMax.y, newMin.y, newMax.y), Scale(val.z, oldMin.z, oldMax.z, newMin.z, newMax.z));
    }

    // returns val scaled between 0 and 1
    // val must be between oldMin and oldMax
    public static float ScaleToUnitInterval(float val, float oldMin, float oldMax)
    {
        return (val - oldMin) / (oldMax - oldMin);
    }
    // returns val scaled between 0 and 1
    // val must be between oldMin and oldMax
    public static float ScaleToUnitInterval(float val, float oldMax)
    {
        return val / oldMax;
    }
    // returns val scaled between 0 and 1
    // val must be between oldMin and oldMax
    public static float3 ScaleToUnitInterval(float3 val, float3 oldMin, float3 oldMax)
    {
        return new float3(ScaleToUnitInterval(val.x, oldMin.x, oldMax.x), ScaleToUnitInterval(val.y, oldMin.y, oldMax.y), ScaleToUnitInterval(val.z, oldMin.z, oldMax.z));
    }
    // returns val scaled between 0 and 1
    // val must be between oldMin and oldMax
    public static float3 ScaleToUnitInterval(float3 val, float3 oldMax)
    {
        return new float3(ScaleToUnitInterval(val.x, oldMax.x), ScaleToUnitInterval(val.y, oldMax.y), ScaleToUnitInterval(val.z, oldMax.z));
    }

    // returns val scaled between -1 and 1
    // val must be between oldMin and oldMax
    public static float ScaleToUnitRange(float val, float oldMin, float oldMax)
    {
        return (((val - oldMin) / (oldMax - oldMin)) * 2) - 1;
    }
    // returns val scaled between -1 and 1
    // val must be between 0 and oldMax
    public static float ScaleToUnitRange(float val, float oldMax)
    {
        return ((val / oldMax) * 2) - 1;
    }

    // returns val scaled between -1 and 1
    // val must be between 0 and 1
    public static float3 ScaleUnitIntervalToUnitRange(float3 val)
    {
        return new float3(ScaleUnitIntervalToUnitRange(val.x), ScaleUnitIntervalToUnitRange(val.y), ScaleUnitIntervalToUnitRange(val.z));
    }

    // returns val scaled betweeen -1 and 1
    // val must be between 0 and 1
    public static float ScaleUnitIntervalToUnitRange(float val)
    {
        //return ((val / 1) * 2) - 1;
        return (val * 2) - 1;
    }

    // returns val scaled between -1 and 1
    // val must be between 0 and oldMax
    public static float3 ScaleToUnitRange(float3 val, float3 oldMax)
    {
        return new float3(ScaleToUnitRange(val.x, oldMax.x), ScaleToUnitRange(val.y, oldMax.y), ScaleToUnitRange(val.z, oldMax.z));
    }
    // returns val scaled between -1 and 1
    // val must be between oldMin and oldMax
    public static float3 ScaleToUnitRange(float3 val, float3 oldMin, float3 oldMax)
    {
        return new float3(ScaleToUnitRange(val.x, oldMin.x, oldMax.x), ScaleToUnitRange(val.y, oldMin.y, oldMax.y), ScaleToUnitRange(val.z, oldMin.z, oldMax.z));
    }


    // returns val scaled between newMin and newMax
    // val MUST be between -1 and 1
    public static float ScaleFromUnitRange(float val, float newMin, float newMax)
    {
        return (((val + 1) * 0.5f) * (newMax - newMin)) + newMin;
    }
    // returns val scaled between 0 and newMax
    // val MUST be between -1 and 1
    public static float ScaleFromUnitRange(float val, float newMax)
    {
        return ((val + 1) * 0.5f) * newMax;
    }
    // returns val scaled between newMin and newMax
    // val MUST be between -1 and 1
    public static float3 ScaleFromUnitRange(float3 val, float3 newMin, float3 newMax)
    {
        return new float3(ScaleFromUnitRange(val.x, newMin.x, newMax.x), ScaleFromUnitRange(val.y, newMin.y, newMax.y), ScaleFromUnitRange(val.z, newMin.z, newMax.z));
    }
    // returns val scaled between newMin and newMax
    // val MUST be between -1 and 1
    public static float3 ScaleFromUnitRange(float3 val, float3 newMax)
    {
        return new float3(ScaleFromUnitRange(val.x, newMax.x), ScaleFromUnitRange(val.y, newMax.y), ScaleFromUnitRange(val.z, newMax.z));
    }

    // returns val scaled between newMin and newMax
    // val MUST be between 0 and 1
    public static float ScaleFromUnitInterval(float val, float newMin, float newMax)
    {
        return (val * (newMax - newMin)) + newMin;
    }
    // returns val scaled between newMin and newMax
    // val MUST be between 0 and 1
    public static float ScaleFromUnitInterval(float val, float newMax)
    {
        return val * newMax;
    }
    // returns val scaled between newMin and newMax
    // val MUST be between 0 and 1
    public static float3 ScaleFromUnitInterval(float3 val, float3 newMin, float3 newMax)
    {
        return new float3(ScaleFromUnitInterval(val.x, newMin.x, newMax.x), ScaleFromUnitInterval(val.y, newMin.y, newMax.y), ScaleFromUnitInterval(val.z, newMin.z, newMax.z));
    }
    // returns val scaled between newMin and newMax
    // val MUST be between 0 and 1
    public static float3 ScaleFromUnitInterval(float3 val, float3 newMax)
    {
        return new float3(ScaleFromUnitInterval(val.x, newMax.x), ScaleFromUnitInterval(val.y, newMax.y), ScaleFromUnitInterval(val.z, newMax.z));
    }

    // returns val scaled between 0 and 1
    // val must be between -1 and 1
    public static float ScaleUnitRangeToUnitInterval(float val)
    {
        return (val + 1) * 0.5f;
    }

    public static float3 ScaleUnitRangeToUnitInterval(float3 val)
    {
        return new float3(ScaleUnitRangeToUnitInterval(val.x), ScaleUnitRangeToUnitInterval(val.y), ScaleUnitRangeToUnitInterval(val.z));
    }

    // RRGGBBAA
    // multiplying by 1/255 converts the return to numbers between 0 and 1
    public static float4 PackUintToRGBA(uint val)
    {
        float4 ret;
        ret.x = (float)(val >> 24) * INV_BYTE; // 000000RR
        ret.y = (float)((val >> 16) & 0xFF) * INV_BYTE; // 000000GG
        ret.z = (float)((val >> 8) & 0xFF) * INV_BYTE; // 000000BB
        ret.w = (float)(val & 0xFF) * INV_BYTE; // 000000AA
        return ret;
    }

    // RRGGBB
    public static half3 PackUshortToRGBAHalf(ushort val)
    {
        half3 ret;
        ret.x = (half)(val >> 11); // 0000RR
        ret.y = (half)(val >> 6); // 0000GG
        ret.z = (half)((byte)val);
        return ret;
    }

    // unpacks an rgba vector to a uint
    static uint UnpackRGBAToUint(float4 v)
    {
        return (((uint)math.round(v.x * 255)) << 24) | (((uint)math.round(v.y * 255)) << 16) | (((uint)math.round(v.z * 255)) << 8) | ((uint)math.round(v.w * 255));
    }
}