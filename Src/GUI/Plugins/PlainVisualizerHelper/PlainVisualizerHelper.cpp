// これは メイン DLL ファイルです。

#include "stdafx.h"

#include "PlainVisualizerHelper.h"

namespace
{

template<typename T>
void CreateImageTemplate(array<array<T>^>^ ar, Byte *ptr, Byte r, Byte g, Byte b)
{
    // 最大・最小の計算
    T min = ar[0][0], max = ar[0][0];

    for (int i = 0; i < ar->Length; i++)
    {
        for (int j = 0; j < ar[i]->Length; j++)
        {
            min = Math::Min(min, ar[i][j]);
            max = Math::Max(max, ar[i][j]);
        }
    }

    const auto range = max - min;
    const double rR = (double)(255 - r) / range, rG = (double)(255 - g) / range, rB = (double)(255 - b) / range;

    for (int i = 0; i < ar->Length; i++)
    {
        for (int j = 0; j < ar[i]->Length; j++)
        {
            auto diff = ar[i][j] - min;

            *ptr = static_cast<Byte>(255 - diff * rB);
            ptr++;

            *ptr = static_cast<Byte>(255 - diff * rG);
            ptr++;

            *ptr = static_cast<Byte>(255 - diff * rR);
            ptr++;

            ptr++;
        }
    }
}

}

void PlainVisualizerHelper::ArrayConverter::CreateImage(Object^ ar, IntPtr image, Byte r, Byte g, Byte b)
{
    auto asByte = dynamic_cast<array<array<Byte>^>^>(ar);
    if (asByte != nullptr)
    {
        return CreateImageTemplate(asByte, (Byte *)image.ToPointer(), r, g, b);
    }

    auto asSByte = dynamic_cast<array<array<SByte>^>^>(ar);
    if (asSByte != nullptr)
    {
        return CreateImageTemplate(asSByte, (Byte *)image.ToPointer(), r, g, b);
    }

    auto asInt32 = dynamic_cast<array<array<Int32>^>^>(ar);
    if (asInt32 != nullptr)
    {
        return CreateImageTemplate(asInt32, (Byte *)image.ToPointer(), r, g, b);
    }

    auto asInt64 = dynamic_cast<array<array<Int64>^>^>(ar);
    if (asInt64 != nullptr)
    {
        return CreateImageTemplate(asInt64, (Byte *)image.ToPointer(), r, g, b);
    }

    auto asDouble = dynamic_cast<array<array<Double>^>^>(ar);
    if (asDouble != nullptr)
    {
        return CreateImageTemplate(asDouble, (Byte *)image.ToPointer(), r, g, b);
    }

    throw gcnew System::Exception();
}
