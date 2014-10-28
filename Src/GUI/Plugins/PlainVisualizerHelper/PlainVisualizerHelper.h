// PlainVisualizerHelper.h

#pragma once

using namespace System;

namespace PlainVisualizerHelper {

public ref class ArrayConverter
{
public:
    static void CreateImage(Object^ ar, IntPtr image, Byte r, Byte g, Byte b);
};

}
