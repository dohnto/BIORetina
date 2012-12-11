using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Emgu.CV;
using System.IO;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.CvEnum;
using BIO.Framework.Core;
using BIO.Framework.Extensions.Emgu.FeatureVector;
using BIO.Framework.Extensions.Emgu.InputData;
using BIO.Framework.Core.FeatureVector;


namespace BIO.Project.BIORetina
{
    class Markant
    {
        public int N {get; set;}
        public Point pos { get; set; }

        public Markant(int N, Point pos)
        {
            this.N = N;
            this.pos = pos;
        }
    }

    class RetinaFeatureVectorExtractor : IFeatureVectorExtractor<EmguGrayImageInputData, EmguMatrixFeatureVector>
    {
        static public Byte VALUE_X = 127;
        static public Byte VALUE_1 = 255;
        static public Byte VALUE_0 = 0;

        public EmguMatrixFeatureVector extractFeatureVector(EmguGrayImageInputData input)
        {
            Image<Gray, Byte> imageOriginal = new Image<Gray, Byte>(input.Bitmap);
            
            Image<Gray, Byte> imageToPlay = imageOriginal.Copy();
            Image<Gray, Byte> imageToSave = imageOriginal.Copy();

            Point blindSpot = _getBlindSpot(imageToPlay);
            
            //var outputImagePath = @"c:\tmp\" + input.FileName.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Last();

            imageToPlay._Erode(3);
            imageToPlay._Dilate(3);
        
            imageToPlay = imageToPlay.ThresholdAdaptive(new Gray(255), ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, THRESH.CV_THRESH_BINARY_INV, 15, new Gray(2));
            Image<Gray, Byte> imageToPlayX = imageToPlay.Copy();

            imageToPlay = _thinning2(imageToPlay);
            

            // remove small connected components
            imageToPlay = _pruning(imageToPlay, 300);

            // prune short branches
            List<Point> endPoints = _getEndPoints(imageToPlay);
            imageToPlay = _pruneWithEndPoints(imageToPlay, endPoints, 50);
            
            //imageToPlay = _correlation(imageToPlay);
            
            //Image<Gray, float> pes = imageToPlay.MatchTemplate(imageToPlay, TM_TYPE.CV_TM_CCORR_NORMED);
            //Console.WriteLine("{0:###}", pes.Size.Width);
            //imageToPlay._Erode(1);
            
            imageToPlay = _centerImage(imageToPlay, blindSpot);
            imageToPlay._Dilate(3);
            Image<Bgr, Byte> imageToDraw = imageToSave.Convert<Bgr, Byte>().ConcateHorizontal(imageToPlay.Convert<Bgr, Byte>());

            
            imageToDraw.Draw(new CircleF(blindSpot,5), new Bgr(Color.Red), 2);
           
            
            //imageToDraw.Save(outputImagePath + ".bmp");

            var featureVector = new EmguMatrixFeatureVector(new Size(imageToPlay.Width - 2, imageToPlay.Height - 2));
            for (int x = 1; x < imageToPlay.Width - 1; x++)
            {
                for (int y = 1; y < imageToPlay.Height - 1; y++)
                {
                    if (imageToPlay.Data[y, x, 0] == VALUE_1)
                        featureVector.FeatureVector[y - 1, x - 1] = VALUE_1;
                    else
                        featureVector.FeatureVector[y - 1, x - 1] = VALUE_0;
                }
            }
            
            return featureVector;
        }

        static public Image<Gray, Byte> _correlation(Image<Gray, Byte> image)
        {
            Image<Gray, Byte> imageToPlay = image.Copy();
            
            Image<Gray, Byte> newImage = image.Copy();
            do
            {
                newImage = imageToPlay;
                // remove small connected components
                newImage = _pruning(newImage, 300);
                
                // ﬁnd junction points
                // ﬁnd end points
                // correct spurious loops
         //       newImage = _removeLoops(newImage);

                // prune short branches
                List<Point> endPoints = _getEndPoints(newImage);
                newImage = _pruneWithEndPoints(newImage, endPoints, 50);
                newImage = _myonly(newImage);
            

                
            } while (imageToPlay == newImage);


            return newImage;
        }

        static public void _applyFilterRotated(Image<Gray, Byte> image, Matrix<Byte> op)
        {
            for (int i = 0; i < 4; ++i)
            {
               _applyKernel(image, op);
                op = _rotateMatrix(op);
            }
            return;
        }

        static public Image<Gray, Byte> _myonly(Image<Gray, Byte> image)
        {
            Image<Gray, Byte> imageToPlay = image.Copy();
            
            Matrix<Byte> operator1 = new Matrix<byte>(3, 3);

            operator1.Data[0, 0] = VALUE_0; operator1.Data[0, 1] = VALUE_0; operator1.Data[0, 2] = VALUE_1;
            operator1.Data[1, 0] = VALUE_0; operator1.Data[1, 1] = VALUE_1; operator1.Data[1, 2] = VALUE_1;
            operator1.Data[2, 0] = VALUE_0; operator1.Data[2, 1] = VALUE_0; operator1.Data[2, 2] = VALUE_1;
            _applyFilterRotated(imageToPlay, operator1);

            return imageToPlay;

        }

        static public List<Point> _findSegmentWithMaxNeigh(Image<Gray, Byte> image, Point startingPixel, int dx, int maxNeigh)
        {
            List<Point> pixelsInSegment = new List<Point>();
            List<Point> toBeExpand = new List<Point>();
            toBeExpand.Add(startingPixel);

            while (toBeExpand.Count != 0)
            {
                Point currentPixel = toBeExpand.ElementAt(0);

                for (int x = -1; x <= 1; x++)
                {
                    if (currentPixel.X + x >= 0 && currentPixel.X + x < image.Width)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            if (currentPixel.Y + y >= 0 && currentPixel.Y + y < image.Height)
                            {
                                Point pixelToExplore = new Point(currentPixel.X + x, currentPixel.Y + y);
                                if (image.Data[pixelToExplore.Y, pixelToExplore.X, 0] == 255)
                                {
                                    if (!pixelsInSegment.Contains(pixelToExplore) && !toBeExpand.Contains(pixelToExplore) && _thin_nonZeroNeigh(image, pixelToExplore) <= maxNeigh)
                                    {
                                         toBeExpand.Add(pixelToExplore);
                                    }
                                }
                            }
                        }
                    }
                }

                pixelsInSegment.Add(currentPixel);
                toBeExpand.RemoveAt(0);

                if (pixelsInSegment.Count > dx)
                    break;
            }
            return pixelsInSegment;
        }


        static public Image<Gray, Byte> _aproximate(Image<Gray, Byte> image, List<Point> endPoints, int dx)
        {
            Image<Gray, Byte> imageToPlay = image.Copy();

            foreach (Point endPoint in endPoints)
            {
                if (_thin_nonZeroNeigh(imageToPlay, endPoint) != 1)
                    continue;

                List<Point> segment = _findSegmentWithMaxNeigh(imageToPlay, endPoint, dx, 2);
                if (segment.Count < dx)
                {
                    foreach (Point p in segment)
                    {
                        imageToPlay.Data[p.Y, p.X, 0] = 0;
                    }
                }
                else
                {
                    //PointF[] pa = new PointF[segment.Count];
                    //int counter = 0;
                    //foreach (Point p in segment)
                    //{
                    //    pa[counter++] = p;
                    //}

                    //PointF a, b;
                    //PointCollection.Line2DFitting(pa, DIST_TYPE.CV_DIST_C, out a, out b);
                    ////LineSegment2D line = new LineSegment2D(b, 
                    ////imageToPlay.Draw(
                    //Console.WriteLine(a.ToString());
                }
            }

            return imageToPlay;

        }

        static public Image<Gray, Byte> _pruneWithEndPoints(Image<Gray, Byte> image, List<Point> endPoints, int dx)
        {
            Image<Gray, Byte> imageToPlay = image.Copy();

            foreach (Point endPoint in endPoints)
            {
                if (_thin_nonZeroNeigh(imageToPlay, endPoint) != 1)
                    continue;

                List<Point> segment = _findSegmentWithMaxNeigh(imageToPlay, endPoint, dx, 2);
                if (segment.Count < dx)
                {
                    foreach (Point p in segment)
                    {
                        imageToPlay.Data[p.Y, p.X, 0] = 0;
                    }
                }
            }

            return imageToPlay;
        }

        static public List<Point> _getEndPoints(Image<Gray, Byte> image)
        {
            List<Point> markants = new List<Point>();

            for (int x = 1; x < image.Width - 1; x++)
            {
                for (int y = 1; y < image.Height - 1; y++)
                {
                    if (image.Data[y, x, 0].Equals(255))
                    {
                        if (_thin_nonZeroNeigh(image, new Point(x, y)) == 1)
                        {
                            markants.Add(new Point(x, y));
                        }
                    }
                }
            }

            return markants;
        }

        static public List<Markant> _getMarkants(Image<Gray, Byte> image)
        {
            List<Markant> markants = new List<Markant>();

            for (int x = 1; x < image.Width-1; x++)
            {
                for (int y = 1; y < image.Height-1; y++)
                {
                    if (image.Data[y, x, 0].Equals(255))
                    {
                        if (_thin_nonZeroNeigh(image, new Point(x, y)) == 1)
                        {
                            markants.Add(new Markant(2, new Point(x, y)));
                        } else if (_thin_nonZeroNeigh(image, new Point(x, y)) == 8)
                        {
                            markants.Add(new Markant(3, new Point(x, y)));
                        }
                    }
                }
            }
            return markants;
        }

        static public Image<Gray, Byte> _pruneOnce(Image<Gray, Byte> image)
        {
            Image<Gray, Byte> imageToPlay = image.Copy();

            const int VALUE_0 = 0;
            const int VALUE_1 = 255;
            const int VALUE_X = 127;

            Matrix<Byte> operator1 = new Matrix<byte>(3, 3);
            operator1.Data[0, 0] = VALUE_X; operator1.Data[0, 1] = VALUE_X; operator1.Data[0, 2] = VALUE_X;
            operator1.Data[1, 0] = VALUE_0; operator1.Data[1, 1] = VALUE_1; operator1.Data[1, 2] = VALUE_0;
            operator1.Data[2, 0] = VALUE_0; operator1.Data[2, 1] = VALUE_0; operator1.Data[2, 2] = VALUE_0;

            Matrix<Byte> operator2 = new Matrix<byte>(3, 3);
            operator2.Data[0, 0] = VALUE_X;     operator2.Data[0, 1] = VALUE_X;     operator2.Data[0, 2] = VALUE_0;
            operator2.Data[1, 0] = VALUE_X;     operator2.Data[1, 1] = VALUE_1;     operator2.Data[1, 2] = VALUE_0;
            operator2.Data[2, 0] = VALUE_0;     operator2.Data[2, 1] = VALUE_0;     operator2.Data[2, 2] = VALUE_0;

            for (int i = 0; i < 4; ++i)
            {
                _applyKernel(imageToPlay, operator1);
                _applyKernel(imageToPlay, operator2);

                operator1 = _rotateMatrix(operator1);
                operator2 = _rotateMatrix(operator2);
            }

            
            return imageToPlay;
        }

        static public Image<Gray, Byte> _findPrunedEdges(Image<Gray, Byte> image)
        {
            
            const int VALUE_0 = 0;
            const int VALUE_1 = 255;
            const int VALUE_X = 127;

            Matrix<Byte> operator1 = new Matrix<byte>(3, 3);
            operator1.Data[0, 0] = VALUE_0; operator1.Data[0, 1] = VALUE_0; operator1.Data[0, 2] = VALUE_0;
            operator1.Data[1, 0] = VALUE_0; operator1.Data[1, 1] = VALUE_1; operator1.Data[1, 2] = VALUE_0;
            operator1.Data[2, 0] = VALUE_0; operator1.Data[2, 1] = VALUE_X; operator1.Data[2, 2] = VALUE_X;

            Matrix<Byte> operator2 = new Matrix<byte>(3, 3);
            operator2.Data[0, 0] = VALUE_0; operator2.Data[0, 1] = VALUE_0; operator2.Data[0, 2] = VALUE_0;
            operator2.Data[1, 0] = VALUE_0; operator2.Data[1, 1] = VALUE_1; operator2.Data[1, 2] = VALUE_0;
            operator2.Data[2, 0] = VALUE_X; operator2.Data[2, 1] = VALUE_X; operator2.Data[2, 2] = VALUE_0;

            Image<Gray, Byte> X2 = new Image<Gray, Byte>(image.Width, image.Height);
            for (int i = 0; i < 4; ++i)
            {
                //X2 = _applyTrueKernel(image, operator1).Or(X2);
                //X2 = _applyTrueKernel(image, operator2).Or(X2);

                _applyKernel(X2, operator1);
                _applyKernel(X2, operator2);

                operator1 = _rotateMatrix(operator1);
                operator2 = _rotateMatrix(operator2);
            }
            return X2;

        }

        static public bool _isPathLong(Image<Gray, Byte> image, Point pixel, List<Point> pixels, int dx)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (pixel.X + x >= 0 && pixel.X +x  < image.Width)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (pixel.Y + y >= 0 && pixel.Y + y < image.Height)
                        {
                            Point pixelToExplore = new Point(pixel.X + x, pixel.Y + y);
                            if (image.Data[pixelToExplore.Y, pixelToExplore.X, 0] == 255)
                            {
                                if (!pixels.Contains(pixelToExplore))
                                {
                                    if (dx == 1)
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        pixels.Add(pixelToExplore);
                                        if (_isPathLong(image, pixelToExplore, pixels, dx - 1))
                                        {
                                            return true;
                                        }
                                        else
                                        {
                                            pixels.Remove(pixelToExplore);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        static public List<Point> _findSegment(Image<Gray, Byte> image, Point startingPixel, int dx)
        {
            List<Point> pixelsInSegment = new List<Point>();
            List<Point> toBeExpand = new List<Point>();
            toBeExpand.Add(startingPixel);

            while (toBeExpand.Count != 0)
            {
                Point currentPixel = toBeExpand.ElementAt(0);

                for (int x = -1; x <= 1; x++)
                {
                    if (currentPixel.X + x >= 0 && currentPixel.X + x < image.Width)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            if (currentPixel.Y + y >= 0 && currentPixel.Y + y < image.Height)
                            {
                                Point pixelToExplore = new Point(currentPixel.X + x, currentPixel.Y + y);
                                if (image.Data[pixelToExplore.Y, pixelToExplore.X, 0] == 255)
                                {
                                    if (startingPixel.X < pixelToExplore.X) return new List<Point>();
                                    if (!pixelsInSegment.Contains(pixelToExplore) && !toBeExpand.Contains(pixelToExplore))
                                    {   
                                        toBeExpand.Add(pixelToExplore);
                                    }
                                }
                            }
                        }
                    }
                }
                
                pixelsInSegment.Add(currentPixel);
                toBeExpand.RemoveAt(0);

                if (pixelsInSegment.Count > dx) 
                    break;
            }
            return pixelsInSegment;
        }

        static public Image<Gray, Byte> _pruning3(Image<Gray, Byte> image, int dx)
        {
            Image<Gray, Byte> X1 = image.Copy();

            for (int i = 0; i < dx; i++)
            {
                X1 = _pruneOnce(X1);
            }
            
            return X1;
        }

        static public Image<Gray, Byte> _pruning(Image<Gray, Byte> image, int dx)
        {
            Image<Gray, Byte> imageToPlay = image.Copy();
            List<Point> savePixels = new List<Point>();

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    if (imageToPlay.Data[y, x, 0].Equals(255))
                    {
                        List<Point> pixels = _findSegment(imageToPlay, new Point(x, y), dx);
                        if (pixels.Count < dx && pixels.Count > 0)
                        {
                            for (int i = 0; i != pixels.Count; ++i)
                            {
                                imageToPlay.Data[pixels.ElementAt(i).Y, pixels.ElementAt(i).X, 0] = 0;
                            }
                        }
                    }
                }
            }
            return imageToPlay;
        }

        static public Image<Gray, Byte> _pruning2(Image<Gray, Byte> image, int dx)
        {
            Image<Gray, Byte> X1 = image.Copy();

            for (int i = 0; i < dx; i++)
            {
                X1 = _pruneOnce(X1);
            }
            Image<Gray, Byte> X2 = _findPrunedEdges(X1);
            Image<Gray, Byte> X3 = X2.Dilate(dx).And(image);
            return X3.Or(X1);
        }


        static public int _thin_Transitions(Image<Gray, Byte> image, Point pixel)
        {
            int sum = 0;
            if (image.Data[pixel.Y - 1, pixel.X - 1, 0] == 255 && image.Data[pixel.Y - 1, pixel.X    , 0] == 0) sum++;
            if (image.Data[pixel.Y - 1, pixel.X, 0] == 255 && image.Data[pixel.Y - 1, pixel.X + 1, 0] == 0) sum++;
            if (image.Data[pixel.Y - 1, pixel.X + 1, 0] == 255 && image.Data[pixel.Y, pixel.X + 1, 0] == 0) sum++;
            if (image.Data[pixel.Y, pixel.X + 1, 0] == 255 && image.Data[pixel.Y + 1, pixel.X + 1, 0] == 0) sum++;
            if (image.Data[pixel.Y + 1, pixel.X + 1, 0] == 255 && image.Data[pixel.Y + 1, pixel.X, 0] == 0) sum++;
            if (image.Data[pixel.Y + 1, pixel.X, 0] == 255 && image.Data[pixel.Y + 1, pixel.X - 1, 0] == 0) sum++;
            if (image.Data[pixel.Y + 1, pixel.X - 1, 0] == 255 && image.Data[pixel.Y, pixel.X - 1, 0] == 0) sum++;
            if (image.Data[pixel.Y, pixel.X - 1, 0] == 255 && image.Data[pixel.Y - 1, pixel.X - 1, 0] == 0) sum++;

            return sum;
        }

        static public int _thin_nonZeroNeigh(Image<Gray, Byte> image, Point pixel)
        {
            int sum = 0;
            if (image.Data[pixel.Y - 1, pixel.X - 1, 0] == 255) sum++;
            if (image.Data[pixel.Y - 1, pixel.X    , 0] == 255) sum++;
            if (image.Data[pixel.Y - 1, pixel.X + 1, 0] == 255) sum++;


            if (image.Data[pixel.Y    , pixel.X - 1, 0] == 255) sum++;
            //if (image.Data[pixel.Y    , pixel.X,     0] == 255) sum++;
            if (image.Data[pixel.Y    , pixel.X + 1, 0] == 255) sum++;


            if (image.Data[pixel.Y + 1, pixel.X - 1, 0] == 255) sum++;
            if (image.Data[pixel.Y + 1, pixel.X,     0] == 255) sum++;
            if (image.Data[pixel.Y + 1, pixel.X + 1, 0] == 255) sum++;

            return sum;
        }

        static public List<Point> _subIter2(Image<Gray, Byte> image)
        {
            List<Point> marked = new List<Point>();

            for (int x = 1; x < image.Width - 1; x++)
            {
                for (int y = 1; y < image.Height - 1; y++)
                {
                    if (image.Data[y, x, 0] == 255)
                    {
                        int N = _thin_nonZeroNeigh(image, new Point(x, y));
                        int S = _thin_Transitions(image, new Point(x, y));

                        if (N >= 2 && 6 >= N && S == 1)
                        {
                            Byte P2 = image.Data[y - 1, x, 0];
                            Byte P4 = image.Data[y, x + 1, 0];
                            Byte P6 = image.Data[y + 1, x, 0];
                            Byte P7 = image.Data[y-1, x - 1, 0];
                            Byte P8 = image.Data[y, x - 1, 0];

                            if (P2 * P4 * P8 == 0 && P2 * P6 * P8 == 0 && P7 != 0)
                            {
                                marked.Add(new Point(x, y));
                            }
                        }
                    }
                }
            }
            return marked;
        }

        static public List<Point> _subIter1(Image<Gray, Byte> image)
        {
            List<Point> marked = new List<Point>();

            for (int x = 1; x < image.Width -1; x++)
            {
                for (int y = 1; y < image.Height-1; y++)
                {
                    if (image.Data[y, x, 0] == 255)
                    {
                        int N = _thin_nonZeroNeigh(image, new Point(x, y));
                        int S = _thin_Transitions(image, new Point(x, y));

                        //Console.WriteLine("N = {0:###}, S = {1:###}", N, S);
                        if (N >= 2 && 6 >= N && S == 1)
                        {
                            Byte P2 = image.Data[y - 1, x, 0];
                            Byte P4 = image.Data[y, x + 1, 0];
                            Byte P6 = image.Data[y + 1, x, 0];
                            Byte P7 = image.Data[y - 1, x - 1, 0];
                            Byte P8 = image.Data[y, x - 1, 0];

                            if (P2 * P4 * P6 == 0 && P4 * P6 * P8 == 0 && P7 != 0)
                            {
                                marked.Add(new Point(x, y));
                            }
                        }
                    }
                }
            }
            return marked;
        }

        static public Image<Gray, Byte> _thinning2(Image<Gray, Byte> image)
        {
            Image<Gray, Byte> imageToPlay1 = image.Copy();

            for (int x = 0; x < image.Width; x++)
            {
                imageToPlay1.Data[0, x, 0] = 0;
                imageToPlay1.Data[image.Height-1, x, 0] = 0;
            }

            for (int y = 0; y < image.Height; y++)
            {
                imageToPlay1.Data[y, 0, 0] = 0;
                imageToPlay1.Data[y, image.Width-1, 0] = 0;
            }

            do
            {
                List<Point> marked1 = _subIter1(imageToPlay1);
                if (marked1.Count == 0) break;
                else
                    foreach (Point pixel in marked1)
                        imageToPlay1.Data[pixel.Y, pixel.X, 0] = 0;

                List<Point> marked2 = _subIter2(imageToPlay1);
                if (marked2.Count == 0) break;
                else
                    foreach (Point pixel in marked2)
                        imageToPlay1.Data[pixel.Y, pixel.X, 0] = 0;
            } while (true);

            
            return imageToPlay1;
        }
        static public Image<Gray, Byte> _thinning(Image<Gray, Byte> image)
        {
            Image<Gray, Byte> imageToPlay = image.Copy();
            
            const int VALUE_0 = 0;
            const int VALUE_1 = 255;
            const int VALUE_X = 127;

            Matrix<Byte> operator1 = new Matrix<byte>(3, 3);
            operator1.Data[0, 0] = VALUE_0;
            operator1.Data[0, 1] = VALUE_0;
            operator1.Data[0, 2] = VALUE_0;
            operator1.Data[1, 0] = VALUE_X; // dont care
            operator1.Data[1, 1] = VALUE_1;
            operator1.Data[1, 2] = VALUE_X; // dont care
            operator1.Data[2, 0] = VALUE_1;
            operator1.Data[2, 1] = VALUE_1;
            operator1.Data[2, 2] = VALUE_1;

            Matrix<Byte> operator2 = new Matrix<byte>(3, 3);
            operator2.Data[0, 0] = VALUE_X; // doesnt care
            operator2.Data[0, 1] = VALUE_0;
            operator2.Data[0, 2] = VALUE_0;
            operator2.Data[1, 0] = VALUE_1;
            operator2.Data[1, 1] = VALUE_1;
            operator2.Data[1, 2] = VALUE_0; // doesnt care
            operator2.Data[2, 0] = VALUE_X;
            operator2.Data[2, 1] = VALUE_1;
            operator2.Data[2, 2] = VALUE_X; // doesnt care

            Image<Gray, Byte> thinnerImage = imageToPlay.Copy();
            do
            {
                imageToPlay = thinnerImage.Copy();

                for (int i = 0; i < 4; ++i)
                {
                    _applyKernel(thinnerImage, operator1);
                    _applyKernel(thinnerImage, operator2);
                    operator1 = _rotateMatrix(operator1);
                    operator2 = _rotateMatrix(operator2);
                }

            } while (! thinnerImage.Equals(imageToPlay));
            
            return imageToPlay;
        }

        static public Matrix<Byte> _rotateMatrix(Matrix<byte> matrix)
        {
            if (matrix.Height != matrix.Width || matrix.Width != 3)
            {
                throw new Exception("Matrix is not 3x3");
            }

            Matrix<Byte> rotatedMatrix = new Matrix<byte>(matrix.Width, matrix.Height);
            rotatedMatrix.Data[0, 0] = matrix.Data[2, 0]; // doesnt care
            rotatedMatrix.Data[0, 1] = matrix.Data[1, 0];
            rotatedMatrix.Data[0, 2] = matrix.Data[0, 0];
            rotatedMatrix.Data[1, 0] = matrix.Data[2, 1];
            rotatedMatrix.Data[1, 1] = matrix.Data[1, 1];
            rotatedMatrix.Data[1, 2] = matrix.Data[0, 1]; // doesnt care
            rotatedMatrix.Data[2, 0] = matrix.Data[2, 2];
            rotatedMatrix.Data[2, 1] = matrix.Data[1, 2];
            rotatedMatrix.Data[2, 2] = matrix.Data[0, 2]; // doesnt care

            return rotatedMatrix;
        }

        static public Image<Gray, Byte> _applyTrueKernel(Image<Gray, Byte> image, Matrix<Byte> kernel)
        {
            Image<Gray, Byte> imageToPlay = image.Copy();
            
            for (int x = 1; x < imageToPlay.Width - 1; x++)
                for (int y = 1; y < imageToPlay.Height - 1; y++)
                {
                    if (_kernelMatches(image, kernel, new Point(x, y)))
                    {
                        imageToPlay.Data[y, x, 0] = 255;
                    }
                    else
                    {
                        imageToPlay.Data[y, x, 0] = 0;
                    }
                }
            return imageToPlay;
        }

        static public int _applyKernel(Image<Gray, Byte> imageToPlay, Matrix<Byte> kernel)
        {
            int changes = 0;
            for (int x = 1; x < imageToPlay.Width - 1; x++)
                for (int y = 1; y < imageToPlay.Height - 1; y++)
                {
                    if (_kernelMatches(imageToPlay, kernel, new Point(x, y)))
                    {
                       imageToPlay.Data[y, x, 0] = VALUE_0;
                    }
                }
            return changes;
        }

        static public bool _kernelMatches(Image<Gray, Byte> image, Matrix<Byte> kernel, Point pixel)
        {
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (! _pixelMatches(image.Data[pixel.Y + y - 1, pixel.X + x - 1, 0], kernel.Data[x, y]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        static public bool _pixelMatches(Byte a, Byte b)
        {
            if (a == 127 || b == 127)
            {
                return true;
            }
            else if (a == b)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }

        static public Point _getBlindSpot(Image<Gray, Byte> image)
        {
            Image<Gray, Byte> imageToPlay = image.Copy();
            imageToPlay = _preprocess(imageToPlay, (float)0.85);

            Point blindSpotMass = _getBlindSpotMass(imageToPlay);
            CircleF tip = _getBlindSpotHough(imageToPlay);
            Point blindSpotHough = new Point((int)tip.Center.X, (int)tip.Center.Y);

            Point tip1 = _reconcileBlindSpot(imageToPlay, blindSpotMass);
            Point tip2 = _reconcileBlindSpot(imageToPlay, blindSpotHough);

            if (Math.Sqrt(Math.Pow(tip1.X - tip2.X, 2) + Math.Pow(tip1.Y - tip2.Y, 2)) > image.Width / 3)
            {
                return _getBlindSpotStupid(imageToPlay);
            }

            if (_avgNeighbourIntensity(imageToPlay, tip1) > _avgNeighbourIntensity(imageToPlay, tip2))
            {
                return tip1;
            }
            else
            {
                return tip2;
            }
        }

        static public Point _reconcileBlindSpot(Image<Gray, Byte> image, Point oldBlindSpot)
        {
            Point newBlindSpot = oldBlindSpot;

            do
            {
                oldBlindSpot = newBlindSpot;
                newBlindSpot = _reconcileBlindSpotOnce(image, oldBlindSpot);
                //Console.WriteLine("old {0:###} -> new {1:###}", _avgNeighbourIntensity(image, oldBlindSpot), _avgNeighbourIntensity(image, newBlindSpot));
            } while (newBlindSpot != oldBlindSpot);

            return newBlindSpot;
        }

        static public Point _reconcileBlindSpotOnce(Image<Gray, Byte> image, Point oldBlindSpot)
        {
            double maxIntensity = _avgNeighbourIntensity(image, oldBlindSpot);
            Point newBlindSpot = oldBlindSpot;

            int dx = 2;
            for (int x = -dx; x < dx + 1; x += dx)
            {
                int newx = oldBlindSpot.X + x;
                for (int y = - dx; y < dx + 1; y += dx)
                {
                    int newy = oldBlindSpot.Y + y;
                    double xyIntensity = _avgNeighbourIntensity(image, new Point(newx, newy));
                    //Console.WriteLine("{0:###}x{1:###} = {2:###}", newx, newy, xyIntensity);
                    if (maxIntensity < xyIntensity)
                    {
                        maxIntensity = xyIntensity;
                        newBlindSpot = new Point(newx, newy);
                    }
                }
            }
            return newBlindSpot;
        }

        static public double _avgNeighbourIntensity(Image<Gray, Byte> image, PointF oldBlindSpot)
        {
            double intensitySum = 0;

            int dx = 3;
            if (oldBlindSpot.X - dx >= 0 && oldBlindSpot.X + dx < image.Width
                && oldBlindSpot.Y - dx >= 0 && oldBlindSpot.Y + dx < image.Height)
            {
                for (int x = -dx; x < dx + 1; x++)
                    for (int y = -dx; y < dx + 1; y++)
                    {
                        int index_x = (int)(oldBlindSpot.X + x);
                        int index_y = (int)(oldBlindSpot.Y + y);

                        intensitySum += image[index_y, index_x].Intensity;
                    }
            }
            else
            {
                //Console.WriteLine("{0:###}", oldBlindSpot.X-dx);
            }
            return intensitySum;
        }

        static public Gray _getMedian(Image<Gray, Byte> image)
        {
            int colors_count = 256;
            int[] histogram = new int[colors_count];
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Gray pixel = image[y, x];
                    histogram[(int)pixel.Intensity] += 1;
                }
            }

            int first_non_zero = 1;
            int last_non_zero = colors_count - 2;

            for (; first_non_zero < colors_count; first_non_zero++)
            {
                if (histogram[first_non_zero] != 0)
                {
                    break;
                }
            }

            for (; last_non_zero >= 0; last_non_zero--)
            {
                if (histogram[last_non_zero] != 0)
                {
                    break;
                }
            }

            return new Gray((last_non_zero-first_non_zero)/2);
        }

        static public Image<Gray, Byte> _preprocess(Image<Gray, Byte> image, PointF center, SizeF size, float percent_of_shown)
        {
            Ellipse mask = new Ellipse(
                            center,
                            new SizeF(size.Width * percent_of_shown, size.Height * percent_of_shown), 90);
            Image<Gray, Byte> withEll = new Image<Gray, Byte>(image.Width, image.Height, new Gray(0));
            withEll.Draw(mask, new Gray(255), -1);
            image._And(withEll);
            return image;
        }

        static public Image<Gray, Byte> _preprocess(Image<Gray, Byte> image, float percent_of_shown)
        {
            return _preprocess(
                image, 
                new Point(image.Width / 2, image.Height / 2), 
                new SizeF(image.Width*percent_of_shown, image.Height*percent_of_shown), 
                percent_of_shown); 
        }

        static public Point _getBlindSpotStupid(Image<Gray, Byte> image)
        {
            Image<Gray, Byte> imageToPlay = image.Copy();
            
            Point tip3 = new Point(0, 0);
            double max = 0;
            for (int x = 2; x < imageToPlay.Width - 2; x++)
            {
                for (int y = 2; y < imageToPlay.Height - 2; y++)
                {
                    if (_avgNeighbourIntensity(imageToPlay, new PointF(x, y)) > max)
                    {
                        max = _avgNeighbourIntensity(imageToPlay, new Point(x, y));
                        tip3 = new Point(x, y);
                    }
                }
            }
            return tip3;
        }

        static public CircleF _getBlindSpotHough(Image<Gray, Byte> image)
        {
            Image<Gray, Byte> image_to_play = image.Copy();

            image_to_play = image_to_play.Dilate(15);
            //image_to_play = image_to_play.Canny(new Gray(50), new Gray(20));
            Gray cannyThreshold = new Gray(50);
            Gray circleAccumulatorThreshold = new Gray(80);
            CircleF[] circles1 = image_to_play.HoughCircles(
                cannyThreshold,
                circleAccumulatorThreshold,
                2.0, //Resolution of the accumulator used to detect centers of the circles
                10.0, //min distance 
                30, //min radius
                140 //max radius
                )[0]; //Get the circles from the first channel
            int i = 0;
            foreach (CircleF circle in circles1)
            {
                float left = circle.Center.X - circle.Radius;
                float right = circle.Center.X + circle.Radius;

                float top = circle.Center.Y - circle.Radius;
                float down = circle.Center.Y + circle.Radius;

                if (left < 0) continue;
                if (top < 0) continue;

                if (right > image_to_play.Width) continue;
                if (down > image_to_play.Height) continue;

                //image_to_play.Draw(circle, new Gray(255), 3);
                //if (i++ > 5) break;
                return circle;
                
            }
            
            //CvInvoke.cvShowImage("pes", image_to_play);
                
            return new CircleF();
        }

        static public Point _getBlindSpotMass(Image<Gray, Byte> image)
        {
            Image<Gray, Byte> image_to_play = image.Copy();
            image_to_play._EqualizeHist();
            image_to_play._ThresholdBinary(new Gray((int)255 * 0.95), new Gray(255));
            for (int i = 0; i < 100; i++)
            {
                int count = 10;
                image_to_play._Erode(count);
                image_to_play._Dilate(count);
            }

            int[] mass_x = new int[image_to_play.Width];
            int sumX = 0;
            int[] mass_y = new int[image_to_play.Height];
            for (int x = 0; x < image_to_play.Width; x++)
            {
                mass_x[x] = 0;
                for (int y = 0; y < image_to_play.Height; y++)
                {
                    if (image_to_play[y, x].Equals(new Gray(255)))
                    {
                        sumX++;
                        mass_x[x] += 1;
                        mass_y[y] += 1;
                    }
                }
            }
            
            double centerX = 0;
            double centerY = 0;
            for (int x = 0; x < image_to_play.Width; x++)
            {
                centerX += mass_x[x] * (double)x / sumX;
            }
            
            for (int y = 0; y < image_to_play.Height; y++)
            {
                centerY += mass_y[y] * (double)y / sumX;
            }
            
            return new Point((int)centerX, (int)centerY);
        }
        
        static public Image<Gray, Byte> _centerImage(Image<Gray, Byte> image, Point center)
        {
            Image<Gray, Byte> imageToPlay = new Image<Gray, Byte>(image.Width, image.Height);
            Point trueCenter = new Point(image.Width/2, image.Height/2);
            
            int dx = center.X - trueCenter.X;
            int dy = center.Y - trueCenter.Y;

            int diff = 5;
            for (int x = diff; x < image.Width-diff; x++)
            {
                for (int y = diff; y < image.Height-diff; y++)
                {
                    int newx = x - dx;
                    int newy = y - dy;


                    if (newx >= 0 && newx < image.Width &&
                        newy >= 0 && newy < image.Height)
                    {
                        imageToPlay.Data[newy, newx, 0] = image.Data[y, x, 0];
                    }
                }
            }


            return imageToPlay;
        }
    }

    class RetinaFeatureVectorExtractor3 : IFeatureVectorExtractor<EmguGrayImageInputData, EmguMatrixFeatureVector>
    {
        public Byte VALUE_X = 127;
        public Byte VALUE_1 = 255;
        public Byte VALUE_0 = 0;

        public EmguMatrixFeatureVector extractFeatureVector(EmguGrayImageInputData input)
        {
            Image<Gray, Byte> imageOriginal = new Image<Gray, Byte>(input.Bitmap);

            Image<Gray, Byte> imageToPlay = imageOriginal.Copy();
            Image<Gray, Byte> imageToSave = imageOriginal.Copy();

            Point blindSpot = RetinaFeatureVectorExtractor._getBlindSpotMass(imageToPlay);
            blindSpot = RetinaFeatureVectorExtractor._reconcileBlindSpot(imageToPlay, blindSpot);
            
            //var outputImagePath = @"c:\tmp\" + input.FileName.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Last();

            imageToPlay._Erode(3);
            imageToPlay._Dilate(3);

            imageToPlay = imageToPlay.ThresholdAdaptive(new Gray(255), ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, THRESH.CV_THRESH_BINARY_INV, 15, new Gray(2));
            Image<Gray, Byte> imageToPlayX = imageToPlay.Copy();

            imageToPlay = RetinaFeatureVectorExtractor._thinning2(imageToPlay);


            // remove small connected components
            imageToPlay = RetinaFeatureVectorExtractor._pruning(imageToPlay, 300);

            // prune short branches
            List<Point> endPoints = RetinaFeatureVectorExtractor._getEndPoints(imageToPlay);
            imageToPlay = RetinaFeatureVectorExtractor._pruneWithEndPoints(imageToPlay, endPoints, 50);

            //imageToPlay = _correlation(imageToPlay);

            //Image<Gray, float> pes = imageToPlay.MatchTemplate(imageToPlay, TM_TYPE.CV_TM_CCORR_NORMED);
            //Console.WriteLine("{0:###}", pes.Size.Width);
            //imageToPlay._Erode(1);

            imageToPlay = RetinaFeatureVectorExtractor._centerImage(imageToPlay, blindSpot);
            imageToPlay._Dilate(3);
            Image<Bgr, Byte> imageToDraw = imageToSave.Convert<Bgr, Byte>().ConcateHorizontal(imageToPlay.Convert<Bgr, Byte>());


            imageToDraw.Draw(new CircleF(blindSpot, 5), new Bgr(Color.Red), 2);


            //imageToDraw.Save(outputImagePath + ".bmp");

            var featureVector = new EmguMatrixFeatureVector(new Size(imageToPlay.Width - 2, imageToPlay.Height - 2));
            for (int x = 1; x < imageToPlay.Width - 1; x++)
            {
                for (int y = 1; y < imageToPlay.Height - 1; y++)
                {
                    if (imageToPlay.Data[y, x, 0] == VALUE_1)
                        featureVector.FeatureVector[y - 1, x - 1] = VALUE_1;
                    else
                        featureVector.FeatureVector[y - 1, x - 1] = VALUE_0;
                }
            }

            return featureVector;
        }
    }

    class RetinaFeatureVectorExtractor4 : IFeatureVectorExtractor<EmguGrayImageInputData, EmguMatrixFeatureVector>
    {
        public Byte VALUE_X = 127;
        public Byte VALUE_1 = 255;
        public Byte VALUE_0 = 0;

        public EmguMatrixFeatureVector extractFeatureVector(EmguGrayImageInputData input)
        {
            Image<Gray, Byte> imageOriginal = new Image<Gray, Byte>(input.Bitmap);

            Image<Gray, Byte> imageToPlay = imageOriginal.Copy();
            Image<Gray, Byte> imageToSave = imageOriginal.Copy();

            Point blindSpot = RetinaFeatureVectorExtractor._getBlindSpotStupid(imageToPlay);
            
            //var outputImagePath = @"c:\tmp\" + input.FileName.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Last();

            imageToPlay._Erode(3);
            imageToPlay._Dilate(3);

            imageToPlay = imageToPlay.ThresholdAdaptive(new Gray(255), ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, THRESH.CV_THRESH_BINARY_INV, 15, new Gray(2));
            Image<Gray, Byte> imageToPlayX = imageToPlay.Copy();

            imageToPlay = RetinaFeatureVectorExtractor._thinning2(imageToPlay);


            // remove small connected components
            imageToPlay = RetinaFeatureVectorExtractor._pruning(imageToPlay, 300);

            // prune short branches
            List<Point> endPoints = RetinaFeatureVectorExtractor._getEndPoints(imageToPlay);
            imageToPlay = RetinaFeatureVectorExtractor._pruneWithEndPoints(imageToPlay, endPoints, 50);

            //imageToPlay = _correlation(imageToPlay);

            //Image<Gray, float> pes = imageToPlay.MatchTemplate(imageToPlay, TM_TYPE.CV_TM_CCORR_NORMED);
            //Console.WriteLine("{0:###}", pes.Size.Width);
            //imageToPlay._Erode(1);

            imageToPlay = RetinaFeatureVectorExtractor._centerImage(imageToPlay, blindSpot);
            imageToPlay._Dilate(3);
            Image<Bgr, Byte> imageToDraw = imageToSave.Convert<Bgr, Byte>().ConcateHorizontal(imageToPlay.Convert<Bgr, Byte>());


            imageToDraw.Draw(new CircleF(blindSpot, 5), new Bgr(Color.Red), 2);


            //imageToDraw.Save(outputImagePath + ".bmp");

            var featureVector = new EmguMatrixFeatureVector(new Size(imageToPlay.Width - 2, imageToPlay.Height - 2));
            for (int x = 1; x < imageToPlay.Width - 1; x++)
            {
                for (int y = 1; y < imageToPlay.Height - 1; y++)
                {
                    if (imageToPlay.Data[y, x, 0] == VALUE_1)
                        featureVector.FeatureVector[y - 1, x - 1] = VALUE_1;
                    else
                        featureVector.FeatureVector[y - 1, x - 1] = VALUE_0;
                }
            }

            return featureVector;
        }
    }


    class RetinaFeatureVectorExtractor2 : IFeatureVectorExtractor<EmguGrayImageInputData, EmguMatrixFeatureVector>
    {
        public Byte VALUE_X = 127;
        public Byte VALUE_1 = 255;
        public Byte VALUE_0 = 0;

        public EmguMatrixFeatureVector extractFeatureVector(EmguGrayImageInputData input)
        {
            Image<Gray, Byte> imageOriginal = new Image<Gray, Byte>(input.Bitmap);

            Image<Gray, Byte> imageToPlay = imageOriginal.Copy();
            Image<Gray, Byte> imageToSave = imageOriginal.Copy();

            CircleF blindSpotAll = RetinaFeatureVectorExtractor._getBlindSpotHough(imageToPlay);
            Point blindSpot = RetinaFeatureVectorExtractor._reconcileBlindSpot(imageToPlay, new Point((int)blindSpotAll.Center.X, (int)blindSpotAll.Center.Y));

            //var outputImagePath = @"c:\tmp\" + input.FileName.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Last();

            imageToPlay._Erode(3);
            imageToPlay._Dilate(3);

            imageToPlay = imageToPlay.ThresholdAdaptive(new Gray(255), ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, THRESH.CV_THRESH_BINARY_INV, 15, new Gray(2));
            Image<Gray, Byte> imageToPlayX = imageToPlay.Copy();

            imageToPlay = RetinaFeatureVectorExtractor._thinning2(imageToPlay);


            // remove small connected components
            imageToPlay = RetinaFeatureVectorExtractor._pruning(imageToPlay, 300);

            // prune short branches
            List<Point> endPoints = RetinaFeatureVectorExtractor._getEndPoints(imageToPlay);
            imageToPlay = RetinaFeatureVectorExtractor._pruneWithEndPoints(imageToPlay, endPoints, 50);

            //imageToPlay = _correlation(imageToPlay);

            //Image<Gray, float> pes = imageToPlay.MatchTemplate(imageToPlay, TM_TYPE.CV_TM_CCORR_NORMED);
            //Console.WriteLine("{0:###}", pes.Size.Width);
            //imageToPlay._Erode(1);

            imageToPlay = RetinaFeatureVectorExtractor._centerImage(imageToPlay, blindSpot);
            imageToPlay._Dilate(3);
            Image<Bgr, Byte> imageToDraw = imageToSave.Convert<Bgr, Byte>().ConcateHorizontal(imageToPlay.Convert<Bgr, Byte>());


            imageToDraw.Draw(new CircleF(blindSpot, 5), new Bgr(Color.Red), 2);


            //imageToDraw.Save(outputImagePath + ".bmp");

            var featureVector = new EmguMatrixFeatureVector(new Size(imageToPlay.Width - 2, imageToPlay.Height - 2));
            for (int x = 1; x < imageToPlay.Width - 1; x++)
            {
                for (int y = 1; y < imageToPlay.Height - 1; y++)
                {
                    if (imageToPlay.Data[y, x, 0] == VALUE_1)
                        featureVector.FeatureVector[y - 1, x - 1] = VALUE_1;
                    else
                        featureVector.FeatureVector[y - 1, x - 1] = VALUE_0;
                }
            }

            return featureVector;
        }
    }
}
