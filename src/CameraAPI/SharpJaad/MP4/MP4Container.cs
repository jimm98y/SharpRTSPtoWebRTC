using SharpJaad.MP4.API;
using SharpJaad.MP4.Boxes;
using SharpJaad.MP4.Boxes.Impl;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharpJaad.MP4
{
    /**
     * The MP4Container is the central class for the MP4 demultiplexer. It reads the
     * container and gives access to the containing data.
     *
     * The data source can be either an <code>InputStream</code> or a
     * <code>RandomAccessFile</code>. Since the specification does not decree a
     * specific order of the content, the data needed for parsing (the sample
     * tables) may be at the end of the stream. In this case, random access is
     * needed and reading from an <code>InputSteam</code> will cause an exception.
     * Thus, whenever possible, a <code>RandomAccessFile</code> should be used for 
     * local files. Parsing from an <code>InputStream</code> is useful when reading 
     * from a network stream.
     *
     * Each <code>MP4Container</code> can return the used file brand (file format
     * version). Optionally, the following data may be present:
     * <ul>
     * <li>progressive download informations: pairs of download rate and playback
     * delay, see {@link #getDownloadInformationPairs() getDownloadInformationPairs()}</li>
     * <li>a <code>Movie</code></li>
     * </ul>
     *
     * Additionally it gives access to the underlying MP4 boxes, that can be 
     * retrieved by <code>getBoxes()</code>. However, it is not recommended to 
     * access the boxes directly.
     * 
     * @author in-somnia
     */
    public class MP4Container
    {
        private readonly MP4InputStream input;
        private readonly List<Box> boxes;
        private Brand major, minor;
        private Brand[] compatible;
        private FileTypeBox ftyp;
        private ProgressiveDownloadInformationBox pdin;
        private Box moov;
        private Movie movie;

        public MP4Container(Stream input)
        {
            this.input = new MP4InputStream(input);
            boxes = new List<Box>();

            ReadContent();
        }

        private void ReadContent()
        {
            //read all boxes
            Box box = null;
            long type;
            bool moovFound = false;
            while (input.hasLeft())
            {
                box = BoxFactory.ParseBox(null, input);
                if (boxes.Count == 0 && box.GetBoxType() != BoxTypes.FILE_TYPE_BOX) throw new MP4Exception("no MP4 signature found");
                boxes.Add(box);

                type = box.GetBoxType();
                if (type == BoxTypes.FILE_TYPE_BOX)
                {
                    if (ftyp == null) ftyp = (FileTypeBox)box;
                }
                else if (type == BoxTypes.MOVIE_BOX)
                {
                    if (movie == null) moov = box;
                    moovFound = true;
                }
                else if (type == BoxTypes.PROGRESSIVE_DOWNLOAD_INFORMATION_BOX)
                {
                    if (pdin == null) pdin = (ProgressiveDownloadInformationBox)box;
                }
                else if (type == BoxTypes.MEDIA_DATA_BOX)
                {
                    if (moovFound) break;
                    else if (!input.hasRandomAccess()) throw new MP4Exception("movie box at end of file, need random access");
                }
            }
        }

        public Brand GetMajorBrand()
        {
            if (major == Brand.UNKNOWN_BRAND) major = BrandExtensions.ForID(ftyp.GetMajorBrand());
            return major;
        }

        public Brand GetMinorBrand()
        {
            if (minor == Brand.UNKNOWN_BRAND) minor = BrandExtensions.ForID(ftyp.GetMajorBrand());
            return minor;
        }

        public Brand[] GetCompatibleBrands()
        {
            if (compatible == null)
            {
                string[] s = ftyp.GetCompatibleBrands();
                compatible = new Brand[s.Length];
                for (int i = 0; i < s.Length; i++)
                {
                    compatible[i] = BrandExtensions.ForID(s[i]);
                }
            }
            return compatible;
        }

        //TODO: pdin, movie fragments??
        public Movie GetMovie()
        {
            if (moov == null) return null;
            else if (movie == null) movie = new Movie(moov, input);
            return movie;
        }

        public List<Box> GetBoxes()
        {
            return boxes.ToList();
        }
    }
}