using System.ComponentModel;
using UnityEngine;

public class ViewBase : MonoBehaviour
{
    protected ViewModelBase _viewModel;

    public virtual void Bind(ViewModelBase viewModel)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnPropertyChanged;
        }

        _viewModel = viewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnPropertyChanged;
        }
    }

    protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // 파생 뷰에서 e.PropertyName 분기 처리
    }

    protected virtual void OnDestroy()
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnPropertyChanged;
        }
    }
}