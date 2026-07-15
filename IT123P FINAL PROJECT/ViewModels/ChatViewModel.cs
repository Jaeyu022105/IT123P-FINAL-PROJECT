using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IT123P_FINAL_PROJECT.Models;
using IT123P_FINAL_PROJECT.Services;

namespace IT123P_FINAL_PROJECT.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private readonly IApiService _api;

        [ObservableProperty] private string _inputText = string.Empty;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _planUpdatedAlertVisible;
        [ObservableProperty] private string _planUpdatedAlertMessage = string.Empty;

        public ObservableCollection<ChatMessageDisplay> Messages { get; } = new();

        public ChatViewModel(IApiService api)
        {
            _api = api;

            // Welcome message
            Messages.Add(new ChatMessageDisplay
            {
                Role = "assistant",
                Text = "Hello! I am your AI Diet Assistant. Ask me anything about healthy eating, meal planning, or ask me to set a diet plan for you (e.g., 'Set a 2000 calorie high protein plan'!).",
                IsUser = false
            });
        }

        private double _proposedCalories;
        private double _proposedProtein;
        private double _proposedCarbs;
        private double _proposedFat;

        [RelayCommand]
        public async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText) || IsBusy) return;

            var userText = InputText.Trim();
            InputText = string.Empty;

            // Add user message
            Messages.Add(new ChatMessageDisplay
            {
                Role = "user",
                Text = userText,
                IsUser = true
            });

            IsBusy = true;
            PlanUpdatedAlertVisible = false;

            try
            {
                // Prepare message history to send to API
                var history = new List<ChatMessageDto>();
                foreach (var msg in Messages)
                {
                    history.Add(new ChatMessageDto
                    {
                        Role = msg.Role,
                        Text = msg.Text
                    });
                }

                // Send request
                var response = await _api.SendChatMessageAsync(history);
                if (response != null)
                {
                    Messages.Add(new ChatMessageDisplay
                    {
                        Role = "assistant",
                        Text = response.Text,
                        IsUser = false
                    });

                    if (response.PlanUpdated)
                    {
                        _proposedCalories = response.ProposedCalories;
                        _proposedProtein = response.ProposedProtein;
                        _proposedCarbs = response.ProposedCarbs;
                        _proposedFat = response.ProposedFat;

                        PlanUpdatedAlertMessage = $"{response.ProposedCalories:F0} kcal | Pro: {response.ProposedProtein:F0}% | Car: {response.ProposedCarbs:F0}% | Fat: {response.ProposedFat:F0}%";
                        PlanUpdatedAlertVisible = true;
                    }
                }
                else
                {
                    Messages.Add(new ChatMessageDisplay
                    {
                        Role = "assistant",
                        Text = "Sorry, I couldn't reach the nutrition server. Please check your connection and try again.",
                        IsUser = false
                    });
                }
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessageDisplay
                {
                    Role = "assistant",
                    Text = $"Error: {ex.Message}",
                    IsUser = false
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ApplyPlanAsync()
        {
            PlanUpdatedAlertVisible = false;
            IsBusy = true;

            try
            {
                var newGoal = new DietGoal
                {
                    Id = 1,
                    DailyCalorieLimit = _proposedCalories,
                    ProteinPercentage = _proposedProtein,
                    CarbsPercentage = _proposedCarbs,
                    FatPercentage = _proposedFat
                };

                var success = await _api.SaveDietGoalAsync(newGoal);
                if (success)
                {
                    Messages.Add(new ChatMessageDisplay
                    {
                        Role = "assistant",
                        Text = $"Awesome! I have updated your daily target to {_proposedCalories:F0} calories.",
                        IsUser = false
                    });

                    // Broadcast message to refresh dashboard
                    MessagingCenter.Send(this, "DietGoalUpdated");
                }
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessageDisplay
                {
                    Role = "assistant",
                    Text = $"Error applying plan: {ex.Message}",
                    IsUser = false
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void DismissPlan()
        {
            PlanUpdatedAlertVisible = false;
        }
    }

    public class ChatMessageDisplay
    {
        public string Role { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool IsUser { get; set; }

        public Microsoft.Maui.Controls.LayoutOptions Alignment => 
            IsUser ? Microsoft.Maui.Controls.LayoutOptions.End : Microsoft.Maui.Controls.LayoutOptions.Start;

        public string BubbleColor => 
            IsUser ? "#1B63C1" : "#2E2E3A"; // Classic premium Blue for user, Dark Grey for AI
            
        public Microsoft.Maui.Graphics.Color TextColor => 
            IsUser ? Microsoft.Maui.Graphics.Colors.White : Microsoft.Maui.Graphics.Colors.White;
    }
}
