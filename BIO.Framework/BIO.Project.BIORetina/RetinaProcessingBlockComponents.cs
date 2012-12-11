using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BIO.Framework.Extensions.Emgu.InputData;
using BIO.Framework.Extensions.Emgu.FeatureVector;
using BIO.Framework.Extensions.Standard.Template;
using BIO.Framework.Extensions.Standard.Block;
using BIO.Framework.Extensions.Standard.Comparator;
using BIO.Framework.Core.Comparator;

namespace BIO.Project.BIORetina
{
    class RetinaProcessingBlockComponents : InputDataProcessingBlockSettings<EmguGrayImageInputData, EmguMatrixFeatureVector,Template<EmguMatrixFeatureVector>,EmguMatrixFeatureVector>
    {
        public RetinaProcessingBlockComponents() : base("blind-spot-all-methods") { 
        
        }

        protected override Framework.Core.FeatureVector.IFeatureVectorExtractor<EmguGrayImageInputData, EmguMatrixFeatureVector> createTemplatedFeatureVectorExtractor()
        {
            return new RetinaFeatureVectorExtractor();
        }

        protected override Framework.Core.FeatureVector.IFeatureVectorExtractor<EmguGrayImageInputData, EmguMatrixFeatureVector> createEvaluationFeatureVectorExtractor()
        {
            return new RetinaFeatureVectorExtractor();
        }

        protected override IComparator<EmguMatrixFeatureVector, Template<EmguMatrixFeatureVector>, EmguMatrixFeatureVector> createComparator()
        {
            return new Comparator<EmguMatrixFeatureVector, Template<EmguMatrixFeatureVector>, EmguMatrixFeatureVector>(
                   CreateFeatureVectorComparator(), CreateScoreSelector());
        }

        private static IFeatureVectorComparator<EmguMatrixFeatureVector, EmguMatrixFeatureVector> CreateFeatureVectorComparator()
        {
            return new RetinaComparator();
        }

        private static IScoreSelector CreateScoreSelector()
        {
            return new MinScoreSelector();
        }
    }

    class RetinaProcessingBlockComponents2 : InputDataProcessingBlockSettings<EmguGrayImageInputData, EmguMatrixFeatureVector, Template<EmguMatrixFeatureVector>, EmguMatrixFeatureVector>
    {
        public RetinaProcessingBlockComponents2()
            : base("blind-spot-hough")
        {

        }

        protected override Framework.Core.FeatureVector.IFeatureVectorExtractor<EmguGrayImageInputData, EmguMatrixFeatureVector> createTemplatedFeatureVectorExtractor()
        {
            return new RetinaFeatureVectorExtractor2();
        }

        protected override Framework.Core.FeatureVector.IFeatureVectorExtractor<EmguGrayImageInputData, EmguMatrixFeatureVector> createEvaluationFeatureVectorExtractor()
        {
            return new RetinaFeatureVectorExtractor2();
        }

        protected override IComparator<EmguMatrixFeatureVector, Template<EmguMatrixFeatureVector>, EmguMatrixFeatureVector> createComparator()
        {
            return new Comparator<EmguMatrixFeatureVector, Template<EmguMatrixFeatureVector>, EmguMatrixFeatureVector>(
                   CreateFeatureVectorComparator(), CreateScoreSelector());
        }

        private static IFeatureVectorComparator<EmguMatrixFeatureVector, EmguMatrixFeatureVector> CreateFeatureVectorComparator()
        {
            return new RetinaComparator();
        }

        private static IScoreSelector CreateScoreSelector()
        {
            return new MinScoreSelector();
        }
    }

    class RetinaProcessingBlockComponents3 : InputDataProcessingBlockSettings<EmguGrayImageInputData, EmguMatrixFeatureVector, Template<EmguMatrixFeatureVector>, EmguMatrixFeatureVector>
    {
        public RetinaProcessingBlockComponents3()
            : base("blind-spot-mass")
        {

        }

        protected override Framework.Core.FeatureVector.IFeatureVectorExtractor<EmguGrayImageInputData, EmguMatrixFeatureVector> createTemplatedFeatureVectorExtractor()
        {
            return new RetinaFeatureVectorExtractor3();
        }

        protected override Framework.Core.FeatureVector.IFeatureVectorExtractor<EmguGrayImageInputData, EmguMatrixFeatureVector> createEvaluationFeatureVectorExtractor()
        {
            return new RetinaFeatureVectorExtractor3();
        }

        protected override IComparator<EmguMatrixFeatureVector, Template<EmguMatrixFeatureVector>, EmguMatrixFeatureVector> createComparator()
        {
            return new Comparator<EmguMatrixFeatureVector, Template<EmguMatrixFeatureVector>, EmguMatrixFeatureVector>(
                   CreateFeatureVectorComparator(), CreateScoreSelector());
        }

        private static IFeatureVectorComparator<EmguMatrixFeatureVector, EmguMatrixFeatureVector> CreateFeatureVectorComparator()
        {
            return new RetinaComparator();
        }

        private static IScoreSelector CreateScoreSelector()
        {
            return new MinScoreSelector();
        }
    }

    class RetinaProcessingBlockComponents4 : InputDataProcessingBlockSettings<EmguGrayImageInputData, EmguMatrixFeatureVector, Template<EmguMatrixFeatureVector>, EmguMatrixFeatureVector>
    {
        public RetinaProcessingBlockComponents4()
            : base("blind-spot-dummy")
        {

        }

        protected override Framework.Core.FeatureVector.IFeatureVectorExtractor<EmguGrayImageInputData, EmguMatrixFeatureVector> createTemplatedFeatureVectorExtractor()
        {
            return new RetinaFeatureVectorExtractor4();
        }

        protected override Framework.Core.FeatureVector.IFeatureVectorExtractor<EmguGrayImageInputData, EmguMatrixFeatureVector> createEvaluationFeatureVectorExtractor()
        {
            return new RetinaFeatureVectorExtractor4();
        }

        protected override IComparator<EmguMatrixFeatureVector, Template<EmguMatrixFeatureVector>, EmguMatrixFeatureVector> createComparator()
        {
            return new Comparator<EmguMatrixFeatureVector, Template<EmguMatrixFeatureVector>, EmguMatrixFeatureVector>(
                   CreateFeatureVectorComparator(), CreateScoreSelector());
        }

        private static IFeatureVectorComparator<EmguMatrixFeatureVector, EmguMatrixFeatureVector> CreateFeatureVectorComparator()
        {
            return new RetinaComparator();
        }

        private static IScoreSelector CreateScoreSelector()
        {
            return new MinScoreSelector();
        }
    }
}
