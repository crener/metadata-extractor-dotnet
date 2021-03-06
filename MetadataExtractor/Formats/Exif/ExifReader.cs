// Copyright (c) Drew Noakes and contributors. All Rights Reserved. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Tiff;
using MetadataExtractor.IO;

#if NET35
using DirectoryList = System.Collections.Generic.IList<MetadataExtractor.Directory>;
#else
using DirectoryList = System.Collections.Generic.IReadOnlyList<MetadataExtractor.Directory>;
#endif

namespace MetadataExtractor.Formats.Exif
{
    /// <summary>
    /// Decodes Exif binary data into potentially many <see cref="Directory"/> objects such as
    /// <see cref="ExifSubIfdDirectory"/>, <see cref="ExifThumbnailDirectory"/>, <see cref="ExifInteropDirectory"/>,
    /// <see cref="GpsDirectory"/>, camera makernote directories and more.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public sealed class ExifReader : IJpegSegmentMetadataReader
    {
        /// <summary>Exif data stored in JPEG files' APP1 segment are preceded by this six character preamble "Exif\0\0".</summary>
        public const string JpegSegmentPreamble = "Exif\x0\x0";

        ICollection<JpegSegmentType> IJpegSegmentMetadataReader.SegmentTypes => new[] { JpegSegmentType.App1 };

        public DirectoryList ReadJpegSegments(IEnumerable<JpegSegment> segments)
        {
            return segments
                .Where(segment => StartsWithJpegExifPreamble(segment.Bytes))
                .SelectMany(segment => Extract(new ByteArrayReader(segment.Bytes, baseOffset: JpegSegmentPreamble.Length)))
                .ToList();
        }

        /// <summary>
        /// Indicates whether <paramref name="bytes"/> starts with <see cref="JpegSegmentPreamble"/>.
        /// </summary>
        public static bool StartsWithJpegExifPreamble(byte[] bytes)
        {
            return bytes.Length >= JpegSegmentPreamble.Length && Encoding.UTF8.GetString(bytes, 0, JpegSegmentPreamble.Length) == JpegSegmentPreamble;
        }

        /// <summary>
        /// Reads TIFF formatted Exif data a specified offset within a <see cref="IndexedReader"/>.
        /// </summary>
        public DirectoryList Extract(IndexedReader reader)
        {
            var directories = new List<Directory>();
            var exifTiffHandler = new ExifTiffHandler(directories);

            try
            {
                // Read the TIFF-formatted Exif data
                TiffReader.ProcessTiff(reader, exifTiffHandler);
            }
            catch (Exception e)
            {
                exifTiffHandler.Error("Exception processing TIFF data: " + e.Message);
            }

            return directories;
        }
    }
}
