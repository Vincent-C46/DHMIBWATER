using DHBIMWATER.Application.Interfaces;

namespace DHBIMWATER.Application.UseCases
{
    public class CountGenericModelUseCase
    {
        private readonly IGenericModelRepository _repository;

        public CountGenericModelUseCase(IGenericModelRepository repository)
        {
            _repository = repository;
        }

        public int Execute()
        {
            var genericModels = _repository.GetAll();
            return genericModels.Count();
        }
    }
}
