from django.contrib import admin
from .models import Post, Comment, Vote

@admin.register(Post)
class PostAdmin(admin.ModelAdmin):
    list_display = ("title", "status", "created_by", "created_at", "vote_count")
    search_fields = ("title", "body", "created_by__username")
    list_filter = ("status", "created_at")

    def vote_count(self, obj):
        return obj.votes.count()

@admin.register(Comment)
class CommentAdmin(admin.ModelAdmin):
    list_display = ("post", "created_by", "created_at")
    search_fields = ("body", "created_by__username", "post__title")

@admin.register(Vote)
class VoteAdmin(admin.ModelAdmin):
    list_display = ("post", "user", "created_at")
    search_fields = ("post__title", "user__username")
