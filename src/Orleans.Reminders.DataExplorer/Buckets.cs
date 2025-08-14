#nullable enable
using Cloudbrick;

namespace Cloudbrick.Orleans.Reminders.DataExplorer;
internal static class Buckets
{
    public static void EnsurePowerOfTwo(int buckets){ if (buckets<=0 || (buckets & buckets-1)!=0) throw new ArgumentException("Buckets must be a power of two."); }
    public static int Bits(int buckets)=> (int)Math.Log2(buckets);
    public static int Index(uint hash,int buckets){ var bits=Bits(buckets); return (int)(hash >> 32 - bits & (uint)(buckets-1)); }
    public static string TableFor(string prefix,int bucket)=> $"{prefix}_{bucket:D3}";
    public static bool RangeIntersectsBucket(uint begin,uint end,int bucket,int buckets)
    { var bits=Bits(buckets); var min=(uint)bucket << 32-bits; var max=min + ((1u << 32-bits) - 1); if (begin<=end) return !(max<begin || min>=end); else return !(max<begin && min>=end); }
    public static bool InRange(uint h,uint begin,uint end)=> begin<=end ? h>=begin && h<end : h>=begin || h<end;
}
