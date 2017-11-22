﻿using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using Thingy.Db.Entity;
using System.Linq;

namespace Thingy.Db
{
    public sealed class DataBase : IDataBase, IDisposable
    {
        private LiteDatabase _db;
        private Stream _filestream;
        private LiteCollection<ToDoItem> _ToDoCollection;

        public DataBase(string file)
        {
            _filestream = File.Open(file, System.IO.FileMode.OpenOrCreate);
            _db = new LiteDatabase(_filestream);
            _ToDoCollection = _db.GetCollection<ToDoItem>(nameof(_ToDoCollection));
        }

        public void Dispose()
        {
            if (_db != null)
            {
                _db.Dispose();
                _db = null;
            }
            if (_filestream != null)
            {
                _filestream.Dispose();
                _filestream = null;
            }
            GC.SuppressFinalize(this);
        }

        public IEnumerable<ToDoItem> GetCompletededTasks()
        {
            return _ToDoCollection.Find(x => x.IsCompleted == true)
                .OrderBy(x => x.DueDate);

        }

        public IEnumerable<ToDoItem> GetUncompletedTasks()
        {
            return _ToDoCollection.Find(x => x.IsCompleted == false);
        }

        public void SaveToDoItem(ToDoItem itemtoSave)
        {
            _ToDoCollection.Insert(itemtoSave);
        }

        public void DeleteToDoItem(ToDoItem toDelete)
        {
            _ToDoCollection.Delete(item => item.Content == toDelete.Content);
        }

        public void UpdateToDoItem(ToDoItem toUpdate)
        {
            var u = (_ToDoCollection.Find(item => item.Content == toUpdate.Content)).FirstOrDefault();
            if (u != null)
            {
                u.CompletedDate = toUpdate.CompletedDate;
                u.Content = toUpdate.Content;
                u.DueDate = toUpdate.DueDate;
                u.IsCompleted = toUpdate.IsCompleted;
                _ToDoCollection.Update(u);
            }
        }
    }
}
