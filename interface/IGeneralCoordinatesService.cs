
using FacultyApi.model;

namespace FacultyApi.Services
{
    public interface IGeneralCoordinatesService
    {
        ApiResponse GetAll();

        ApiResponse GetByUserID(int userid);

        ApiResponse Insert(GeneralCoordinates model);

        ApiResponse Update(GeneralCoordinates model);

        ApiResponse Delete(int code);
    }
}