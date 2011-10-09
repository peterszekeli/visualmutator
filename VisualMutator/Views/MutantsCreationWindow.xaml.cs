﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VisualMutator.Views
{
    using CommonUtilityInfrastructure.WpfUtils;

    public interface IMutantsCreationView : IView
    {
        bool? ShowDialog();

        void Close();
        
    }

    /// <summary>
    /// Interaction logic for MutantsCreationWindow.xaml
    /// </summary>
    public partial class MutantsCreationWindow : Window, IMutantsCreationView
    {
        public MutantsCreationWindow()
        {
            InitializeComponent();
         
        }
    }
}
