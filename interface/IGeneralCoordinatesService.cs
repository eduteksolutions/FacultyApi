using FacultyApi.model;

namespace FacultyApi.Internal
{
    public interface IGeneralCoordinatesService
    {
     
            ApiResponse GetByUserID(int userid);
        
    }
}