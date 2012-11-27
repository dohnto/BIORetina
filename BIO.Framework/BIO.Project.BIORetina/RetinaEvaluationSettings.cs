using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BIO.Framework.Core.Evaluation;
using BIO.Framework.Core.Template.Persistence;
using BIO.Framework.Extensions.Standard.Template.Persistence;
using BIO.Framework.Extensions.Standard.Database.InputDatabase;
using BIO.Framework.Extensions.Emgu.InputData;


namespace BIO.Project.BIORetina
{
    class RetinaEvaluationSettings : BIO.Framework.Extensions.Standard.Evaluation.Block.BlockEvaluationSettings<
        StandardRecord<StandardRecordData>, //standard database record
        EmguGrayImageInputData
    >
    {
        public RetinaEvaluationSettings() 
        {
            {
                //var value = new FaceProcessingBlockComponents();
                //this.addBlockToEvaluation(value.createBlock());
            }
        }

        protected override Framework.Core.InputData.IInputDataCreator<StandardRecord<StandardRecordData>, EmguGrayImageInputData> createInputDataCreator() {
            //return new FaceInputDataCreator();
            return null;
        }

    }
}
