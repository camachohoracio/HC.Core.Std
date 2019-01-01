namespace HC.Core.Io
{
    public enum DataFileType
    {
        Excel,
        Csv,
        Txt
    }

    public static class Constants
    {
        public const string STR_BAT_FILE_NAME = "bat_tmp.bat";

        public const string STR_DATA_FILES_FILTER = @"Excel files (*.xls)|*.xls" +
                                                    @"|Text files (*.txt)|*.txt" +
                                                    @"|Csv files (*.csv)|*.csv" +
                                                    @"|All files (*.*)|*.* ";

        public const string STR_IMAGE_FILES_FILTER = @"JPEG Images (*.jpg,*.jpeg)|*.jpg;*.jpeg" +
                                                     @"|Gif Images (*.gif)|*.gif" +
                                                     @"|Bitmaps (*.bmp)|*.bmp" +
                                                     @"|All files (*.*)|*.* ";
    }
}


