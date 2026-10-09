
using BeautyManager.Models;

namespace BeautyManager.Data;

public interface IDatabase
{

    int Add(Client client);

    bool NameExists(string name);

    bool PhoneExists(string phone);


    IReadOnlyList<Client> GetFiltered(string? search);
}
