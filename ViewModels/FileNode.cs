using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeaIndexer.ViewModels
{
    public class FileNode : CheckableTreeNode<FileNode>
    {
        public string FullPath { get; set; }
        public bool IsFile { get; set; }

        public FileNode()
        {
            IsChecked = true;
        }

    }
}
