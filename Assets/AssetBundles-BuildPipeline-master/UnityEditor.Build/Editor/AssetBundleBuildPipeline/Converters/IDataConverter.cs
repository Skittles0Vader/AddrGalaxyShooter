using UnityEditor.Build.Utilities;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public interface IDataConverter
    {
        uint Version { get; }

        bool UseCache { get; set; }

        IProgressTracker ProgressTracker { get; set; }
    }

    public abstract class ADataConverter : IDataConverter
    {
        public abstract uint Version { get; }

        public virtual bool UseCache { get; set; }

        public virtual IProgressTracker ProgressTracker { get; set; }

        public ADataConverter(bool useCache, IProgressTracker progressTracker)
        {
            UseCache = useCache;
            ProgressTracker = progressTracker;
        }

        public virtual void StartProgressBar(string title, int progressCount)
        {
            if (ProgressTracker == null)
                return;

            ProgressTracker.StartStep(title, progressCount);
        }

        public virtual bool UpdateProgressBar(string info)
        {
            if (ProgressTracker == null)
                return true;

            return ProgressTracker.UpdateProgress(info);
        }

        public virtual bool EndProgressBar()
        {
            if (ProgressTracker == null)
                return true;

            return ProgressTracker.EndProgress();
        }
    }

    public abstract class ADataConverter<I, O> : ADataConverter
    {
        public ADataConverter(bool useCache, IProgressTracker progressTracker) : base(useCache, progressTracker) { }

        public abstract BuildPipelineCodes Convert(I input, out O output);
    }

    public abstract class ADataConverter<I1, I2, O1> : ADataConverter
    {
        public ADataConverter(bool useCache, IProgressTracker progressTracker) : base(useCache, progressTracker) { }

        public abstract BuildPipelineCodes Convert(I1 input, I2 input2, out O1 output);
    }

    public abstract class ADataConverter<I1, I2, I3, O1> : ADataConverter
    {
        public ADataConverter(bool useCache, IProgressTracker progressTracker) : base(useCache, progressTracker) { }

        public abstract BuildPipelineCodes Convert(I1 input, I2 input2, I3 input3, out O1 output);
    }

    public abstract class ADataConverter<I1, I2, I3, I4, O1> : ADataConverter
    {
        public ADataConverter(bool useCache, IProgressTracker progressTracker) : base(useCache, progressTracker) { }

        public abstract BuildPipelineCodes Convert(I1 input, I2 input2, I3 input3, I4 input4, out O1 output);
    }
}