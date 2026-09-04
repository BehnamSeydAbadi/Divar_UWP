using System;
using Divar_UWP.Models;

namespace Divar_UWP.Infrastructure
{
    public interface IDivarSelectionStore
    {
        event EventHandler CityChanged;

        DivarCity GetSelectedCity();

        void SaveSelectedCity(DivarCity city);

        string GetSelectedCategoryToken();

        void SaveSelectedCategoryToken(string token);
    }
}
