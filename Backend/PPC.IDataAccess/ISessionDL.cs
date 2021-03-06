﻿using System.Collections.Generic;
using PPC.Domain;

namespace PPC.IDataAccess
{
    public interface ISessionDL
    {
        // Insert/Update could be replaced with SaveTransaction(ShopTransaction transaction)
        //void InsertTransaction(ShopTransaction transaction);
        //void UpdateTransaction(ShopTransaction transaction);
        //void DeleteTransaction(ShopTransaction transaction);
        //void SaveTransaction(ShopTransaction transaction);
        void SaveTransactions(IEnumerable<ShopTransaction> transactions); // TODO: remove

        void SaveClientCart(ClientCart clientCart);
        void DeleteClientCart(ClientCart clientCart);

        void SaveNotes(string notes);

        bool HasActiveSession();
        void CreateActiveSession();
        Session GetActiveSession();
        void CloseActiveSession();
    }
}
