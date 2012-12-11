using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.Flann;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.CvEnum;
using System.Drawing;

using BIO.Framework.Core.Comparator;
using BIO.Framework.Extensions.Emgu.FeatureVector;


namespace BIO.Project.BIORetina
{
    class RetinaComparator : IFeatureVectorComparator<EmguMatrixFeatureVector, EmguMatrixFeatureVector>
    {
        public MatchingScore computeMatchingScore(EmguMatrixFeatureVector extracted, EmguMatrixFeatureVector templated)
        {
            if (extracted.FeatureVector.Size != templated.FeatureVector.Size)
                throw new ArgumentException("Feature vector and template mismatch.");

            
            Image<Gray, Byte> a = new Image<Gray, Byte>(extracted.FeatureVector.Cols, extracted.FeatureVector.Rows);
            extracted.FeatureVector.CopyTo(a);

            Image<Gray, Byte> b = new Image<Gray, Byte>(templated.FeatureVector.Cols, templated.FeatureVector.Rows);
            templated.FeatureVector.CopyTo(b);

            double score = _hammingTryAll(a, b);
            //double score = _hamming(extracted.FeatureVector, templated.FeatureVector);
                
            return new MatchingScore(score);
        }

        private double _hammingTryAll(Image<Gray, Byte> a, Image<Gray, Byte> b)
        {
            double max = 0;

            for (int angle = -10; angle <= 10; angle++)
            {
                double current = _hamming(a.Rotate(angle, new Gray(0)), b);
                if (current > max)
                {
                    max = current;
                }
            }

            return max;
        }


        private double _hamming(Image<Gray, Byte> a, Image<Gray, Byte> b)
        {
            if (a.Size != b.Size)
                throw new ArgumentException("Feature vector and template mismatch.");
     
            double sumA = a.GetSum().Intensity;
            double sumB = b.GetSum().Intensity;
            
            Image<Gray, Byte> result = a.And(b);
            result._And(b);

            if (sumA > sumB)
            {

                return result.GetSum().Intensity / sumA;
            }
            else
            {
                return result.GetSum().Intensity / sumB;
            }
        }
    }
}
