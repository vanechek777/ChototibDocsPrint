using System.Windows.Controls;
using System.Windows;
using ChtotibDocsPrintNET.ViewModels;
using ChtotibDocsPrintNET.Models;
namespace ChtotibDocsPrintNET.Views
{
    public partial class AdminPage : UserControl
    {
        public AdminPage() { InitializeComponent(); }

        private void SubjectsGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (DataContext is not AdminViewModel vm) return;
            if (e.Row.Item is not Subject subject) return;
            vm.SaveSubjectSpecialty(subject);
        }

        private void SpecialtiesGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (DataContext is not AdminViewModel vm) return;
            if (e.Row.Item is not Specialty specialty) return;
            vm.SaveSpecialty(specialty);
        }

        private void CommissionGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (DataContext is not AdminViewModel vm) return;
            if (e.Row.Item is not CommissionMember member) return;
            vm.SaveCommissionMember(member);
        }
    }
}
