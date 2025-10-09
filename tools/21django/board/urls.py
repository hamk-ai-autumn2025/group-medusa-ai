from django.urls import path
from .views import PostListView, PostDetailView, PostCreateView, add_comment, toggle_vote

urlpatterns = [
    path("", PostListView.as_view(), name="post_list"),
    path("post/<int:pk>/", PostDetailView.as_view(), name="post_detail"),
    path("post/new/", PostCreateView.as_view(), name="post_create"),
    path("post/<int:pk>/comment/", add_comment, name="add_comment"),
    path("post/<int:pk>/vote/", toggle_vote, name="toggle_vote"),
]
