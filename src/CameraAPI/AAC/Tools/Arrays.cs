namespace CameraAPI.AAC.Tools
{
    internal class Arrays
    {
        public static void Fill<T>(T[] array, T value)
        {
            Fill<T>(array, 0, array.Length, value);
        }

        public static void Fill<T>(T[] array, int fromIndex, int toIndex, T value)
        {
            for (int i = fromIndex; i < toIndex; i++)
            {
                array[i] = value;
            }
        }
    }
}
