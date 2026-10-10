namespace TLinkProxy.Abstractions
{
    public interface IAiModelStore
    {
        void AddModel(IAiModel model);

        IAiModel GetModel(string alias);

        IReadOnlyList<IAiModel> GetAllModels();

        bool DeleteModel(IAiModel model);

        bool UpdateModel(IAiModel model);
    }
}
