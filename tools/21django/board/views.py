from django.contrib import messages
from django.contrib.auth.decorators import login_required
from django.contrib.auth.mixins import LoginRequiredMixin
from django.db.models import Count
from django.http import HttpResponseForbidden, HttpResponseRedirect
from django.shortcuts import get_object_or_404, redirect, render
from django.urls import reverse, reverse_lazy
from django.views.generic import ListView, DetailView, CreateView

from .forms import PostForm, CommentForm
from .models import Post, Vote

class PostListView(ListView):
    model = Post
    template_name = "board/post_list.html"
    context_object_name = "posts"
    paginate_by = 10

    def get_queryset(self):
        qs = (
            Post.objects.all()
            .annotate(num_comments=Count("comments"), num_votes=Count("votes"))
            .order_by("-num_votes", "-created_at")
        )
        q = self.request.GET.get("q")
        if q:
            qs = qs.filter(title__icontains=q)
        status = self.request.GET.get("status")
        if status in {"open", "closed"}:
            qs = qs.filter(status=status)
        return qs

class PostDetailView(DetailView):
    model = Post
    template_name = "board/post_detail.html"
    context_object_name = "post"

    def get_context_data(self, **kwargs):
        ctx = super().get_context_data(**kwargs)
        ctx["comment_form"] = CommentForm()
        return ctx

class PostCreateView(LoginRequiredMixin, CreateView):
    model = Post
    form_class = PostForm
    template_name = "board/post_form.html"
    success_url = reverse_lazy("post_list")

    def form_valid(self, form):
        form.instance.created_by = self.request.user
        messages.success(self.request, "Post created.")
        return super().form_valid(form)

@login_required
def add_comment(request, pk):
    post = get_object_or_404(Post, pk=pk)
    if request.method != "POST":
        return HttpResponseForbidden("POST required")
    form = CommentForm(request.POST)
    if form.is_valid():
        c = form.save(commit=False)
        c.created_by = request.user
        c.post = post
        c.save()
        messages.success(request, "Comment added.")
    return redirect("post_detail", pk=pk)

@login_required
def toggle_vote(request, pk):
    post = get_object_or_404(Post, pk=pk)
    v = Vote.objects.filter(post=post, user=request.user).first()
    if v:
        v.delete()
        messages.info(request, "Vote removed.")
    else:
        Vote.objects.create(post=post, user=request.user)
        messages.success(request, "Voted.")
    return HttpResponseRedirect(reverse("post_detail", args=[pk]))
